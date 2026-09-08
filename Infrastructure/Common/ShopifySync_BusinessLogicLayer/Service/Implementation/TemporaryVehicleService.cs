using AutoMapper;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;
using ShopifySync_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;
using ClosedXML.Excel;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_DataAccessLayer.Entities.Supplier;
using static ShopifySync_DataAccessLayer.Enum.ImportFitmentStatusEnum;
using X.PagedList;
using System.Security.Claims;
using ShopifySync_DataAccessLayer.Entities.Authentication.Register;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class TemporaryVehicleService : ITemporaryVehicleService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;

		private readonly IS3StorageService _storageService;
		private readonly ShopifySyncDbContext _context;
        private readonly ITemporaryVehicleRepository _repository;
        private readonly IMapper _mapper;
        private readonly int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public TemporaryVehicleService(
            IConfiguration configuration,
            ICommonService commonService,
			IS3StorageService storageService,
			ShopifySyncDbContext context,
            ITemporaryVehicleRepository repository,
            IMapper mapper
        )
        {
            _configuration = configuration;
            _commonService = commonService;
			_storageService = storageService;
			_repository = repository;
            _context = context;
            _mapper = mapper;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion


        public async Task<Response> GetTempVehicleList(TempVehicleFilterModel filterModel)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var tempImportResponse = await _repository.GetTempVehicleList(filterModel);
                var result = _mapper.Map<IEnumerable<TemporaryVehicleDTO>>(tempImportResponse);
             
                int totalItemCount = tempImportResponse is IPagedList pagedList
                    ? pagedList.TotalItemCount
                    : result.Count();

                int perPage = DefaultPageSize;
                if (filterModel.per_page?.ToLower() == "all")
                {
                    perPage = totalItemCount;
                }
                else if (!string.IsNullOrEmpty(filterModel.per_page))
                {
                    int.TryParse(filterModel.per_page, out int parsedPageSize);
                    if (parsedPageSize > 0)
                    {
                        perPage = parsedPageSize;
                    }
                }

                int pageNo = filterModel.page_no.HasValue ? filterModel.page_no.Value : 1;
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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleTypeDetailsMessage, result, currentUserRole, pagination);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetTempVehicleList", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> GetTempVehicleDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var tempImportResponse = await _repository.GetTempVehicleDetailsById(id);
                var result = _mapper.Map<TemporaryVehicleDTO>(tempImportResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.VehicleTypeDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleTypeDetailsMessage, result, currentUserRole);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetTempVehicleDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewTempVehicle(TemporaryVehicleCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var tempImportDetails = _mapper.Map<TempVehicleImports>(dto);
                dto.created_at = DateTime.UtcNow;
                await _repository.CreateNewTempVehicle(tempImportDetails);
                var result = _mapper.Map<TemporaryVehicleDTO>(tempImportDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewTempVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateTempVehicle(TemporaryVehicleUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var tempImportDetails = await _repository.GetTempVehicleDetailsById(dto.TempVehicleImportId);
                _mapper.Map(dto, tempImportDetails);
                dto.updated_at = DateTime.UtcNow;
                await _repository.UpdateTempVehicle(tempImportDetails);
                var result = _mapper.Map<TemporaryVehicleDTO>(tempImportDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsUpdateMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateTempVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteTempVehicle(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteTempVehicle(id);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteTempVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        #region Upload File To Import Fitment Table
        public async Task<Response> UploadFileToImportFitment(VehicleExcelFileModel model)
        {
            Response response = new Response();
           // var filePath = "";
            try
            {
				var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
				//var filePath = Path.Combine(Path.GetFullPath("ExcelFiles"), "uploads");
				var fileName = Path.GetFileNameWithoutExtension(model.File.FileName);
				var extension = Path.GetExtension(model.File.FileName);
				var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
				var s3Key = Settings.AWS_S3_EXCEL_FOLDER() + $"{uniqueFileName}";

				#region Checking for file validation that it is excel file or not
				var allowedExtensions = Settings.GetAllowedExcelExtensions();
				var allowedContentTypes = Settings.GetAllowedExcelContentTypes();

				var contentType = model.File.ContentType?.ToLower();

				if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(contentType))
				{
					return ResponseHelper.BadRequest(_ApiResponseMessageList.ExcelExtensionErrorMessage);
				}

				#endregion

				

				// fullPath = Path.Combine(filePath, uniqueFileName);

				// Checking if uploaded file has valid header or not
				using (var memoryStream = new MemoryStream())
                {
                    await model.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using (var workbook = new XLWorkbook(memoryStream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var headers = worksheet.Row(1).Cells().Select(c => c.GetValue<string>().Trim()).ToList();
                        var requiredHeaders = new List<string> { "OEM", "Year", "Make", "Model", "Category" };

                        var missingHeaders = requiredHeaders
                            .Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        if (missingHeaders.Any())
                        {
                            return ResponseHelper.BadRequest(_ApiResponseMessageList.UploadedFileNotProperErrorMessage);
                        }
                    }

					memoryStream.Position = 0;

                    // s3 bucket code is commented because currently given bucket is wrong 
                    var isFileUploaded = await _storageService.UploadFileToS3Bucket(memoryStream, s3Key, model.File.ContentType);
				
					if (!isFileUploaded)
					{
						return ResponseHelper.InternalServerError(_ApiResponseMessageList.S3UploadFailErrorMessage);
					}
					
                   // Directory.CreateDirectory(filePath); // Ensure directory exists

                    //using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    //{
                    //    await memoryStream.CopyToAsync(fileStream);
                    //}

                    // Get Current User Id
                    var currentUserId = CommonFunction.GetUserDataByToken("UserId");

                    ImportFitment importFitment = new()
                    {
                        FileLink = s3Key,
                        UserId = currentUserId,
                        Status = Status.Imported.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repository.CreateNewImportFitment(importFitment);

                    return ResponseHelper.Success(_ApiResponseMessageList.FileUploadedSuccessMessage,null,currentUserRole);

                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UploadFileToImportHistory", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        #endregion


        #region Copy Excel Data InTo Temp Vehicle Table

        public async Task<Response> ProcessFile()
        {
            Response response = new Response();
            List<TempVehicleImports> tempVehicleImport = new List<TempVehicleImports>();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #region Getting the excel file which is in progress
                var getContexData = _context.ImportFitment.Where(x => x.Status == Status.Imported.ToString()).FirstOrDefault();

				getContexData.FileLink = string.IsNullOrEmpty(getContexData.FileLink) ? null : getContexData.FileLink;
				#endregion

				if (getContexData != null)
                {
                    #region Adding Excel file data in TempVehicleImport table

                    using (var stream = new MemoryStream())
                    {
                        var filePath = getContexData.FileLink;
						// Check if file exists
						if (filePath == null)
						{
							throw new FileNotFoundException(_ApiResponseMessageList.ExcelFileNotFoundErrorMessage);
						}

                        // s3 bucket code is commented because currently given bucket is wrong 
                        var getUploadedFile = await _storageService.GetFileFromS3Bucket(filePath); // CommonFunction.FetchFileFromS3Bucket(filePath);

                        // Check if file exists
                        if (getUploadedFile == null)
                            throw new FileNotFoundException(_ApiResponseMessageList.ExcelFileNotInS3ErrorMessage + filePath);

                        
						// Reset the stream position to beginning (important)
						getUploadedFile.Position = 0;
						using (var workbook = new XLWorkbook(getUploadedFile))
                        {
                            var worksheet = workbook.Worksheet(1);
                            var rowCount = worksheet.LastRowUsed().RowNumber(); // Total rows with data


                            if ((bool)getContexData.IsDefault)
                            {
                                for (int row = 2; row <= rowCount; row++) // Skip header row (row 1)
                                {
                                    var oem = worksheet.Cell(row, 1).GetValue<string>().Trim();
                                    var year = worksheet.Cell(row, 2).GetValue<string>().Trim();
                                    var make = worksheet.Cell(row, 3).GetValue<string>().Trim();
                                    var model = worksheet.Cell(row, 4).GetValue<string>().Trim();
                                    var category = worksheet.Cell(row, 5).GetValue<string>().Trim();
                                    var supplier = worksheet.Cell(row, 6).GetValue<string>().Trim();


                                    var tempImportHistory = new TempVehicleImports
                                    {
                                        ImportFitmentId = getContexData.ImportFitmentId,
                                        type = category,
                                        year = Convert.ToInt32(year),
                                        make = make,
                                        model = model,
                                        type_id = 0,
                                        year_id = 0,
                                        make_id = 0,
                                        model_id = 0,
                                        is_default_type = true,
                                        is_default_year = true,
                                        is_default_make = true,
                                        is_default_model = true,
                                        is_processed = true,
                                        created_at = DateTime.UtcNow

                                    };
                                    tempVehicleImport.Add(tempImportHistory);
                                }


                                //#region Inserting data in the master table
                                //AddTempDataToMasterTable(tempVehicleImport, getContexData.SupplierId);
                                //#endregion

                            }
                            else
                            {
                                for (int row = 2; row <= rowCount; row++) // Skip header row (row 1)
                                {
                                    var oem = worksheet.Cell(row, 1).GetValue<string>().Trim();
                                    var year = worksheet.Cell(row, 2).GetValue<string>().Trim();
                                    var make = worksheet.Cell(row, 3).GetValue<string>().Trim();
                                    var model = worksheet.Cell(row, 4).GetValue<string>().Trim();
                                    var category = worksheet.Cell(row, 5).GetValue<string>().Trim();
                                    var supplier = worksheet.Cell(row, 6).GetValue<string>().Trim();


                                    #region Step 1: Checking For Existing Data

                                    var typeId = _context.VehicleTypes.FirstOrDefault(x => x.Name.ToLower().Contains(category.ToLower()))?.VehicleTypesId;
                                    var yearId = _context.VehicleYears.FirstOrDefault(x => x.Name.ToLower().Contains(year.ToLower()))?.VehicleYearId;
                                    var makeId = _context.VehicleMakes.FirstOrDefault(x => x.Name.ToLower().Contains(make.ToLower()))?.VehicleMakeId;
                                    var modelId = _context.VehicleModels.FirstOrDefault(x => x.Name.ToLower().Contains(model.ToLower()))?.VehicleModelId;

                                    bool isParentAvailable = typeId != null || typeId != null || makeId != null || modelId != null;

                                    #endregion

                                    var tempImportHistory = new TempVehicleImports
                                    {
                                        ImportFitmentId = getContexData.ImportFitmentId,
                                        type = category,
                                        year = Convert.ToInt32(year),
                                        make = make,
                                        model = model,
                                        type_id = (typeId != null) ? typeId : 0,
                                        year_id = (yearId !=null) ? yearId : 0,
                                        make_id = (makeId != null) ? makeId : 0,
                                        model_id = (modelId != null) ? modelId : 0,
                                        is_default_type = (typeId == null) ? true : false,
                                        is_default_year = (yearId == null) ? true : false,
                                        is_default_make = (makeId == null) ? true : false,
                                        is_default_model = (modelId == null) ? true : false,
                                        is_processed = false, //(isParentAvailable == true) ? false : true,
                                        created_at = DateTime.UtcNow
                                    };

                                    tempVehicleImport.Add(tempImportHistory);
                                }

                                

                                //#region Inserting data in the master table
                                //AddTempDataToMasterTable(tempVehicleImport, getContexData.SupplierId);
                                //#endregion
                            }

                        }
                        #endregion
                    }

                    if (tempVehicleImport.Count > 0)
                    {
                        await _repository.BulkCreateTempVehicle(tempVehicleImport);
                    }
                }

                return ResponseHelper.Success(_ApiResponseMessageList.ProcessFileMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ProcessFile", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        #endregion

        public async Task<Response> BulkUpdateVehicleAttribute(BulkUpdateVehicleAttributeDTO dto)
        {
            Response response = new Response();
            try
            {
                // Validate attribute type
                var validAttributeTypes = new[] { "type", "make", "year", "model" };
                if (!validAttributeTypes.Contains(dto.AttributeType.ToLower()))
                {
                    return ResponseHelper.BadRequest("Invalid attribute type. Valid types are: type, make, year, model");
                }

                // Validate that either NewAttributeId is provided OR SetAsDefault is true, but not both
                if (dto.NewAttributeId.HasValue && dto.SetAsDefault)
                {
                    return ResponseHelper.BadRequest("Cannot set both NewAttributeId and SetAsDefault. Choose one option.");
                }

                if (!dto.NewAttributeId.HasValue && !dto.SetAsDefault)
                {
                    return ResponseHelper.BadRequest("Either NewAttributeId must be provided or SetAsDefault must be true.");
                }

                // Validate that the ImportFitmentId exists
                var importFitmentExists = _context.ImportFitment.Any(x => x.ImportFitmentId == dto.TempVehicleImportId);
                if (!importFitmentExists)
                {
                    return ResponseHelper.BadRequest("ImportFitmentId not found.");
                }

                // If NewAttributeId is provided, validate that the ID exists in the corresponding master table
                if (dto.NewAttributeId.HasValue)
                {
                    bool idExists = false;
                    switch (dto.AttributeType.ToLower())
                    {
                        case "type":
                            idExists = _context.VehicleTypes.Any(x => x.VehicleTypesId == dto.NewAttributeId.Value);
                            break;
                        case "make":
                            idExists = _context.VehicleMakes.Any(x => x.VehicleMakeId == dto.NewAttributeId.Value);
                            break;
                        case "year":
                            idExists = _context.VehicleYears.Any(x => x.VehicleYearId == dto.NewAttributeId.Value);
                            break;
                        case "model":
                            idExists = _context.VehicleModels.Any(x => x.VehicleModelId == dto.NewAttributeId.Value);
                            break;
                    }

                    if (!idExists)
                    {
                        return ResponseHelper.BadRequest($"The provided {dto.AttributeType} ID does not exist in the master table.");
                    }
                }

                // Perform the bulk update
                var updatedCount = await _repository.BulkUpdateVehicleAttribute(
                    dto.TempVehicleImportId,
                    dto.AttributeType,
                    dto.AttributeValue,
                    dto.NewAttributeId,
                    dto.SetAsDefault
                );

                if (updatedCount == 0)
                {
                    return ResponseHelper.BadRequest($"No records found matching the criteria. ImportFitmentId: {dto.TempVehicleImportId}, {dto.AttributeType}: {dto.AttributeValue}");
                }

                var resultMessage = dto.NewAttributeId.HasValue
                    ? $"Successfully updated {updatedCount} records with {dto.AttributeType}_id = {dto.NewAttributeId.Value}"
                    : $"Successfully updated {updatedCount} records with is_default_{dto.AttributeType} = true";

                return ResponseHelper.Success(resultMessage, new { UpdatedRecordsCount = updatedCount });
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "BulkUpdateVehicleAttribute", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
    }
}
