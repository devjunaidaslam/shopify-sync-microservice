using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using X.PagedList.EF;
using static PartFinderMicroServices_DataAccessLayer.Entities.Vehicle.VehicleModel;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class VehicleRepository : IVehicleRepository
    {
        #region Fields
        private readonly IConfiguration _configuration;

        private readonly PartFinderDbContext _context;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public VehicleRepository(
            IConfiguration configuration,
            PartFinderDbContext context
        )
        {
            _configuration = configuration;
            _context = context;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<IEnumerable<VehicleDTO>> GetAllVehicleList(VehicleFilterModel vehicleFilterModel)
        {
            IEnumerable<VehicleDTO> items = null;
            var query = from v in _context.Vehicles
                        join vt in _context.VehicleTypes on v.TypeId equals vt.VehicleTypesId
                        join vm in _context.VehicleModels on v.ModelId equals vm.VehicleModelId
                        join vy in _context.VehicleYears on v.YearId equals vy.VehicleYearId
                        join vma in _context.VehicleMakes on v.MakeId equals vma.VehicleMakeId
                        join vo in _context.OemVehicles on v.VehicleId equals vo.VehicleId
                        where (
                        (vehicleFilterModel.TypeId > 0 ? v.TypeId == vehicleFilterModel.TypeId : true) &&
                        (vehicleFilterModel.ModelId > 0 ? v.ModelId == vehicleFilterModel.ModelId : true) &&
                        (vehicleFilterModel.MakeId > 0 ? v.MakeId == vehicleFilterModel.MakeId : true) &&
                        (vehicleFilterModel.YearId > 0 ? v.YearId == vehicleFilterModel.YearId : true)
                        )
                        select new
                        {
                            v,
                            vt,
                            vm,
                            vy,
                            vma,
                            vo
                        };

            if (!string.IsNullOrWhiteSpace(vehicleFilterModel.search))
            {
                query = query.Where(x =>
                    x.vt.Name.ToLower().Contains(vehicleFilterModel.search.ToLower()) ||
                    x.vm.Name.ToLower().Contains(vehicleFilterModel.search.ToLower()) ||
                    x.vy.Name.ToLower().Contains(vehicleFilterModel.search.ToLower()) ||
                    x.vma.Name.ToLower().Contains(vehicleFilterModel.search.ToLower()) ||
                    x.vo.OEMId.ToString().Contains(vehicleFilterModel.search));
            }

            // Apply OEM filter: only include vehicles that have a non-deleted link to the specified OEM
            if (vehicleFilterModel.OemId.HasValue)
            {
                int oemId = vehicleFilterModel.OemId.Value;
                query = query.Where(x => _context.OemVehicles.Any(ov =>
                    ov.VehicleId == x.v.VehicleId &&
                    ov.OEMId == oemId &&
                    ov.DeletedAt == null));
            }

            if (vehicleFilterModel.per_page != null && vehicleFilterModel.per_page.ToLower() == "all")
            {
                items = await query
                        .OrderBy(x => x.v.VehicleId)
                        .Select(x => new VehicleDTO
                        {
                            VehicleId = x.v.VehicleId,
                            TypeName = x.vt.Name,
                            ModelName = x.vm.Name,
                            YearName = x.vy.Name,
                            MakeName = x.vma.Name,
                            IsActive = x.v.IsActive
                        }).ToListAsync();
            }
            else
            {
                int perPage = DefaultPageSize;

                if (vehicleFilterModel.per_page.IsNotNullOrEmpty())
                {
                    int.TryParse(vehicleFilterModel.per_page, out perPage);
                }
                vehicleFilterModel.page_no = vehicleFilterModel.page_no > 0 ? vehicleFilterModel.page_no : 1;

                items = await query
                .OrderBy(x => x.v.VehicleId)
                .Select(x => new VehicleDTO
                {
                    VehicleId = x.v.VehicleId,
                    TypeName = x.vt.Name,
                    ModelName = x.vm.Name,
                    YearName = x.vy.Name,
                    MakeName = x.vma.Name,
                    IsActive = x.v.IsActive
                })
                .ToPagedListAsync((int)vehicleFilterModel.page_no, perPage);

            }

            return items;
        }

        public async Task<VehicleDTO> GetVehicleDetailsById(long id)
        {
            var query = from v in _context.Vehicles
                        join vt in _context.VehicleTypes on v.TypeId equals vt.VehicleTypesId
                        join vm in _context.VehicleModels on v.ModelId equals vm.VehicleModelId
                        join vy in _context.VehicleYears on v.YearId equals vy.VehicleYearId
                        join vma in _context.VehicleMakes on v.MakeId equals vma.VehicleMakeId
                        where v.IsActive == true && v.VehicleId == id
                        select new
                        {
                            v,
                            vt,
                            vm,
                            vy,
                            vma
                        };


            var items = await query
                .OrderBy(x => x.v.VehicleId)
                .Select(x => new VehicleDTO
                {
                    VehicleId = x.v.VehicleId,
                    TypeName = x.vt.Name,
                    ModelName = x.vm.Name,
                    YearName = x.vy.Name,
                    MakeName = x.vma.Name,
                    IsActive = x.v.IsActive
                })
                .FirstOrDefaultAsync();

            return items;
        }


        public async Task<Vehicles> GetVehicleDetailsByIdForDelete(long id) =>
            await _context.Vehicles.FindAsync(id);


        public async Task CreateNewVehicle(Vehicles vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVehicle(Vehicles vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicle(long vehicleId)
        {
            var vehicle = await GetVehicleDetailsByIdForDelete(vehicleId);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<VehicleTypes>> GetAllVehicleTypes()
        {
            return await _context.VehicleTypes.ToListAsync();
        }

        public async Task<IEnumerable<VehicleYears>> GetAllVehicleYears()
        {
            return await _context.VehicleYears.ToListAsync();
        }

        public async Task<IEnumerable<VehicleMakes>> GetAllVehicleMakes()
        {
            return await _context.VehicleMakes.ToListAsync();
        }

        public async Task<IEnumerable<VehicleModels>> GetAllVehicleModels()
        {
            return await _context.VehicleModels.ToListAsync();
        }

        public async Task<IEnumerable<VehicleTypes>> GetVehicleTypesByTypeId(long typeId)
        {
            var types = await _context.Vehicles
                .AsQueryable()
                .Join(_context.VehicleTypes,
                      v => v.TypeId,
                      t => t.VehicleTypesId,
                      (v, t) => t)
                .Distinct()
                .OrderBy(t => t.Name ?? string.Empty)
                .ToListAsync();

            return types ?? new List<VehicleTypes>();
        }

        public async Task<IEnumerable<VehicleYears>> GetYearsByTypeId(long typeId)
        {
            var years = await _context.Vehicles
                .Where(v => v.TypeId == typeId)
                .Join(_context.VehicleYears,
                      v => v.YearId,
                      y => y.VehicleYearId,
                      (v, y) => y)
                .Distinct()
                .OrderBy(y => y.Name ?? string.Empty)
                .ToListAsync();

            return years ?? new List<VehicleYears>();
        }

        public async Task<IEnumerable<VehicleMakes>> GetMakesByTypeIdAndYearId(long typeId, long yearId)
        {
            var makes = await _context.Vehicles
                .Where(v => v.TypeId == typeId && v.YearId == yearId)
                .Join(_context.VehicleMakes,
                      v => v.MakeId,
                      m => m.VehicleMakeId,
                      (v, m) => m)
                .Distinct()
                .OrderBy(m => m.Name ?? string.Empty)
                .ToListAsync();

            return makes ?? new List<VehicleMakes>();
        }

        public async Task<IEnumerable<VehicleModels>> GetModelsByTypeIdYearIdAndMakeId(long typeId, long yearId, long makeId)
        {
            var models = await _context.Vehicles
                .Where(v => v.TypeId == typeId && v.YearId == yearId && v.MakeId == makeId)
                .Join(_context.VehicleModels,
                      v => v.ModelId,
                      m => m.VehicleModelId,
                      (v, m) => m)
                .Distinct()
                .OrderBy(m => m.Name ?? string.Empty)
                .ToListAsync();

            return models ?? new List<VehicleModels>();
        }

        public async Task<Vehicles> GetVehicle(long typeId, long makeId, long yearId, long modelId)
        {
            return await _context.Vehicles
                .FirstOrDefaultAsync(v =>
                    v.TypeId == typeId &&
                    v.MakeId == makeId &&
                    v.YearId == yearId &&
                    v.ModelId == modelId);
        }

        public async Task<VehicleTypes> CreateVehicleType(VehicleTypes type)
        {
            _context.VehicleTypes.Add(type);
            await _context.SaveChangesAsync();
            return type;
        }

        public async Task<VehicleYears> CreateVehicleYear(VehicleYears year)
        {
            _context.VehicleYears.Add(year);
            await _context.SaveChangesAsync();
            return year;
        }

        public async Task<VehicleMakes> CreateVehicleMake(VehicleMakes make)
        {
            _context.VehicleMakes.Add(make);
            await _context.SaveChangesAsync();
            return make;
        }

        public async Task<VehicleModels> CreateVehicleModel(VehicleModels model)
        {
            _context.VehicleModels.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<int> UpdateResourceActiveStatus(long resourceId, string resourceType, bool isActive)
        {
            int affectedVehicles = 0;

            switch (resourceType.ToLower())
            {
                case "type":
                    var vehicleType = await _context.VehicleTypes.FindAsync(resourceId);
                    if (vehicleType != null)
                    {
                        vehicleType.IsActive = isActive;

                        var vehiclesWithType = await _context.Vehicles
                            .Where(v => v.TypeId == resourceId)
                            .ToListAsync();

                        foreach (var vehicle in vehiclesWithType)
                        {
                            vehicle.IsActive = isActive;
                        }

                        affectedVehicles = vehiclesWithType.Count;
                    }
                    break;

                case "year":
                    var vehicleYear = await _context.VehicleYears.FindAsync(resourceId);
                    if (vehicleYear != null)
                    {
                        vehicleYear.IsActive = isActive;

                        var vehiclesWithYear = await _context.Vehicles
                            .Where(v => v.YearId == resourceId)
                            .ToListAsync();

                        foreach (var vehicle in vehiclesWithYear)
                        {
                            vehicle.IsActive = isActive;
                        }

                        affectedVehicles = vehiclesWithYear.Count;
                    }
                    break;

                case "make":
                    var vehicleMake = await _context.VehicleMakes.FindAsync(resourceId);
                    if (vehicleMake != null)
                    {
                        vehicleMake.IsActive = isActive;

                        var vehiclesWithMake = await _context.Vehicles
                            .Where(v => v.MakeId == resourceId)
                            .ToListAsync();

                        foreach (var vehicle in vehiclesWithMake)
                        {
                            vehicle.IsActive = isActive;
                        }

                        affectedVehicles = vehiclesWithMake.Count;
                    }
                    break;

                case "model":
                    var vehicleModel = await _context.VehicleModels.FindAsync(resourceId);
                    if (vehicleModel != null)
                    {
                        vehicleModel.IsActive = isActive;

                        var vehiclesWithModel = await _context.Vehicles
                            .Where(v => v.ModelId == resourceId)
                            .ToListAsync();

                        foreach (var vehicle in vehiclesWithModel)
                        {
                            vehicle.IsActive = isActive;
                        }

                        affectedVehicles = vehiclesWithModel.Count;
                    }
                    break;

                default:
                    throw new ArgumentException($"Invalid resource type: {resourceType}");
            }

            await _context.SaveChangesAsync();
            return affectedVehicles;
        }
    }
}
