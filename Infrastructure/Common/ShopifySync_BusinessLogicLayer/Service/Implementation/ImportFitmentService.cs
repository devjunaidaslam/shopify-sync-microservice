using AutoMapper;
using Microsoft.Extensions.Configuration;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.MakeDTO;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.DTOs.ImportFitment;
using ShopifySync_BusinessLogicLayer.Functions;
using System.Security.Claims;
using X.PagedList;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using System.IO;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using ShopifySync_BusinessLogicLayer.Jobs;
 using ShopifySync_DataAccessLayer.Enum;
 using ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class ImportFitmentService : IImportFitmentService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;
        private readonly IS3StorageService _storageService;

        private readonly IImportFitmentRepository _importFitmentRepository;
        private readonly IMapper _mapper;
        private readonly PreprocessFitmentJob _preprocessJob;
        private readonly FinalizeFitmentJob _finalizeJob;
        private readonly IFitmentBackgroundQueue _fitmentQueue;
        private int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        
        /// <summary>
        /// Ensures all DateTime properties in the ImportFitment entity have Kind=UTC to prevent PostgreSQL errors
        /// </summary>
        private void EnsureUtcDateTimes(ImportFitment importFitment)
        {
            // Handle nullable DateTime properties - all are nullable in the entity
            if (importFitment.CreatedAt.HasValue && importFitment.CreatedAt.Value != DateTime.MinValue)
                importFitment.CreatedAt = DateTime.SpecifyKind(importFitment.CreatedAt.Value, DateTimeKind.Utc);
            
            if (importFitment.UpdatedAt.HasValue && importFitment.UpdatedAt.Value != DateTime.MinValue)
                importFitment.UpdatedAt = DateTime.SpecifyKind(importFitment.UpdatedAt.Value, DateTimeKind.Utc);
            
            if (importFitment.CleanedAt.HasValue && importFitment.CleanedAt.Value != DateTime.MinValue)
                importFitment.CleanedAt = DateTime.SpecifyKind(importFitment.CleanedAt.Value, DateTimeKind.Utc);
        }
        #endregion

        #region Constructor
        public ImportFitmentService(
            IConfiguration configuration,
            ICommonService commonService,
            IImportFitmentRepository importFitmentRepository,
            IMapper mapper,
            IS3StorageService storageService,
            PreprocessFitmentJob preprocessJob,
            FinalizeFitmentJob finalizeJob,
            IFitmentBackgroundQueue fitmentQueue
        )
        {
            _configuration = configuration;
            _commonService = commonService;
            _importFitmentRepository = importFitmentRepository;
            _mapper = mapper;
            _storageService = storageService;
            _preprocessJob = preprocessJob;
            _finalizeJob = finalizeJob;
            _fitmentQueue = fitmentQueue;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion


        public async Task<Response> GetImportFitmentList(int pageNo, string pageSize)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var importFitmentResponse = await _importFitmentRepository.GetImportFitmentList(pageNo, pageSize);
                var result = _mapper.Map<IEnumerable<ImportFitmentDTO>>(importFitmentResponse);

                // Update FileLink to return the full S3 URL for each item
                foreach (var item in result)
                {
                    if (!string.IsNullOrEmpty(item.FileLink))
                    {
                        item.FileLink = await _storageService.GetPresignedUrl(item.FileLink, 1);
                    }
                }

                int totalItemCount = importFitmentResponse is IPagedList pagedList
                   ? pagedList.TotalItemCount
                   : result.Count();

                int perPage = DefaultPageSize;
                if (!string.IsNullOrEmpty(pageSize) && pageSize.ToLower() != "all")
                {
                    int.TryParse(pageSize, out perPage);
                }
                else
                {
                    perPage = totalItemCount;
                }

                pageNo = pageNo > 0 ? pageNo : 1;
                int totalPages = perPage > 0 ? (int)Math.Ceiling((double)totalItemCount / perPage) : 0;

                var pagination = new PaginationInfo
                {
                    TotalItemCount = totalItemCount,
                    PageNo = pageNo,
                    PerPage = perPage,
                    TotalPages = totalPages,
                    NextPage = pageNo < totalPages ? pageNo + 1 : 0,
                    PrevPage = pageNo > 1 ? pageNo - 1 : 0
                };

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleMakeDetailsMessage, result, currentUserRole, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetImportFitmentList", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> GetImportFitmentDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var importFitmentResponse = await _importFitmentRepository.GetImportFitmentDetailsById(id);
                var result = _mapper.Map<ImportFitmentDTO>(importFitmentResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.ImportFitmentNotFoundMessage);
                }

                // Transform relative FileLink path to full S3 URL
                if (!string.IsNullOrEmpty(result.FileLink))
                {
                    result.FileLink = await _storageService.GetPresignedUrl(result.FileLink, 1);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchImportFitmentMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetImportFitmentDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewImportFitment(ImportFitmentCreateDTO dto)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var importFitmentDetails = _mapper.Map<ImportFitment>(dto);
                EnsureUtcDateTimes(importFitmentDetails); // Ensure all DateTime fields are UTC
                await _importFitmentRepository.CreateNewImportFitment(importFitmentDetails);
                var result = _mapper.Map<ImportFitmentDTO>(importFitmentDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.ImportFitmentRemoveMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> CreateNewImportFitmentWithFile(ImportFitmentFileUploadDTO fileUploadDto)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var currentUserId = CommonFunction.GetUserDataByToken("UserId");

                if (fileUploadDto.File == null || fileUploadDto.File.Length == 0)
                {
                    return ResponseHelper.BadRequest("No file was uploaded");
                }

                // Check file extension
                var extension = Path.GetExtension(fileUploadDto.File.FileName).ToLowerInvariant();
                if (extension != ".csv" && extension != ".xlsx")
                {
                    return ResponseHelper.BadRequest("Only CSV and XLSX files are supported");
                }

                // Process the file
                var memoryStream = new MemoryStream();
                try
                {
                    await fileUploadDto.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Count the number of lines in the file
                    long lineCount = 0;
                    if (extension == ".csv")
                    {
                        using (var reader = new StreamReader(memoryStream, leaveOpen: true))
                        {
                            while (reader.ReadLine() != null)
                            {
                                lineCount++;
                            }
                        }
                    }
                    else // .xlsx
                    {
                        // For Excel files, we need to use ClosedXML
                        using (var workbook = new XLWorkbook(memoryStream))
                        {
                            var worksheet = workbook.Worksheet(1);
                            lineCount = worksheet.LastRowUsed().RowNumber();
                        }
                    }

                    // Reset stream position for upload
                    memoryStream.Position = 0;

                    // Generate unique filename
                    var fileName = Path.GetFileNameWithoutExtension(fileUploadDto.File.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                    var s3Key = Settings.AWS_S3_EXCEL_FOLDER() + uniqueFileName;

                    // Upload to S3
                    var isFileUploaded = await _storageService.UploadFileToS3Bucket(memoryStream, s3Key, fileUploadDto.File.ContentType);
                    if (!isFileUploaded)
                    {
                        return ResponseHelper.BadRequest("Failed to upload file to S3");
                    }

                    // Create import history record
                    var importFitmentDto = new ImportFitmentCreateDTO
                    {
                        SupplierId = fileUploadDto.SupplierId,
                        FileLink = s3Key, // Store only the relative path in DB
                        ProcessedLines = lineCount > 0 ? lineCount - 1 : 0, // Subtract header row
                        UserId = currentUserId,
                        Status = ImportFitmentStatuses.Imported,
                        IsDefault = fileUploadDto.IsDefault,
                        CreatedAt = DateTime.UtcNow
                    };

                    var importFitmentDetails = _mapper.Map<ImportFitment>(importFitmentDto);
                    EnsureUtcDateTimes(importFitmentDetails); // Ensure all DateTime fields are UTC
                    await _importFitmentRepository.CreateNewImportFitment(importFitmentDetails);
                    importFitmentDetails.Status = ImportFitmentStatuses.PreprocessingQueued;
                    importFitmentDetails.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                    EnsureUtcDateTimes(importFitmentDetails);
                    await _importFitmentRepository.UpdateImportFitment(importFitmentDetails);
                    // Auto-trigger preprocessing after import creation:
                    _fitmentQueue.Enqueue(new FitmentJobRequest(importFitmentDetails.ImportFitmentId, FitmentJobType.Preprocess));

                    var result = _mapper.Map<ImportFitmentDTO>(importFitmentDetails);

                    // Return the full S3 URL to the frontend
                    result.FileLink = !string.IsNullOrEmpty(result.FileLink)
                        ? await _storageService.GetPresignedUrl(result.FileLink, 1)
                        : string.Empty;

                    return ResponseHelper.Success(_ApiResponseMessageList.ImportFitmentRemoveMessage, result, currentUserRole);
                }
                finally
                {
                    memoryStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewImportFitmentWithFile", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> UpdateImportFitment(ImportFitmentUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var importFitmentDetails = await _importFitmentRepository.GetImportFitmentDetailsById(dto.ImportFitmentId);
                _mapper.Map(dto, importFitmentDetails);
                EnsureUtcDateTimes(importFitmentDetails); // Ensure all DateTime fields are UTC
                await _importFitmentRepository.UpdateImportFitment(importFitmentDetails);
                var result = _mapper.Map<ImportFitmentDTO>(importFitmentDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.ImportFitmentDetailsUpdateMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteImportFitment(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _importFitmentRepository.DeleteImportFitment(id);

                return ResponseHelper.Success(_ApiResponseMessageList.ImportFitmentRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> PreprocessImportFitment(long importFitmentId)
        {
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var importFitment = await _importFitmentRepository.GetImportFitmentDetailsById(importFitmentId);

                if (importFitment == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.ImportFitmentNotFoundMessage);
                }

                // Set status to PreprocessingQueued and enqueue background job
                importFitment.Status = ImportFitmentStatuses.PreprocessingQueued;
                importFitment.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                EnsureUtcDateTimes(importFitment);
                await _importFitmentRepository.UpdateImportFitment(importFitment);

                _fitmentQueue.Enqueue(new FitmentJobRequest(importFitmentId, FitmentJobType.Preprocess));
                return ResponseHelper.Success("Import fitment preprocessing queued to run in background", null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "PreprocessImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> FinalizeImportFitment(long importFitmentId)
        {
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var import = await _importFitmentRepository.GetImportFitmentDetailsById(importFitmentId);
                if (import == null)
                {
                    return ResponseHelper.NotFound($"Import fitment with ID {importFitmentId} not found");
                }

                if (import.Status != ImportFitmentStatuses.Preprocessed &&
                    import.Status != ImportFitmentStatuses.Finalizing &&
                    import.Status != ImportFitmentStatuses.FinalizingQueued)
                {
                    return ResponseHelper.BadRequest($"Import fitment {importFitmentId} is not in 'Preprocessed' status");
                }

                // Set status to FinalizingQueued and enqueue background job
                import.Status = ImportFitmentStatuses.FinalizingQueued;
                import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                EnsureUtcDateTimes(import);
                await _importFitmentRepository.UpdateImportFitment(import);

                _fitmentQueue.Enqueue(new FitmentJobRequest(importFitmentId, FitmentJobType.Finalize));
                return ResponseHelper.Success("Import fitment finalization queued to run in background", null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "FinalizeImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> ProcessImportFitment(long importFitmentId)
        {
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var import = await _importFitmentRepository.GetImportFitmentDetailsById(importFitmentId);
                if (import == null)
                {
                    return ResponseHelper.NotFound($"Import fitment with ID {importFitmentId} not found");
                }

                if (import.Status != ImportFitmentStatuses.Imported &&
                    import.Status != ImportFitmentStatuses.Preprocessing &&
                    import.Status != ImportFitmentStatuses.PreprocessingQueued)
                {
                    return ResponseHelper.BadRequest($"Import fitment {importFitmentId} is not in 'Imported' status");
                }

                // Enqueue the combined process (preprocess then finalize) and set status to PreprocessingQueued
                import.Status = ImportFitmentStatuses.PreprocessingQueued;
                import.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                EnsureUtcDateTimes(import);
                await _importFitmentRepository.UpdateImportFitment(import);

                _fitmentQueue.Enqueue(new FitmentJobRequest(importFitmentId, FitmentJobType.ProcessAll));
                return ResponseHelper.Success("Import fitment processing queued to run in background", null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ProcessImportFitment", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
    }
}
