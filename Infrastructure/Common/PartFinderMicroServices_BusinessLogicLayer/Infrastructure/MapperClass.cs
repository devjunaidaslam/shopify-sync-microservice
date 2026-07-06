using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.YearDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TypeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.MakeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ModelDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.Supplier;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ImportFitment;

namespace PartFinderMicroServices_BusinessLogicLayer.Infrastructure
{
    public class MapperClass : Profile
    {
        public MapperClass()
        {
            #region Vehicle Year 
            CreateMap<VehicleYears, YearDTO>();
            CreateMap<YearCreateDTO, VehicleYears>();
            CreateMap<YearUpdateDTO, VehicleYears>();
            #endregion

            #region Vehicle Type 
            CreateMap<VehicleTypes, TypeDTO>();
            CreateMap<TypeCreateDTO, VehicleTypes>();
            CreateMap<TypeUpdateDTO, VehicleTypes>();
            #endregion

            #region Vehicle Make 
            CreateMap<VehicleMakes, MakeDTO>();
            CreateMap<MakeCreateDTO, VehicleMakes>();
            CreateMap<MakeUpdateDTO, VehicleMakes>();
            #endregion

            #region Vehicle Model
            CreateMap<VehicleModels, ModelDTO>();
            CreateMap<ModelCreateDTO, VehicleModels>();
            CreateMap<ModelUpdateDTO, VehicleModels>();
            #endregion


            #region Vehicle
            CreateMap<Vehicles, VehicleDTO>();
            CreateMap<VehicleCreateDTO, Vehicles>();
            CreateMap<VehicleUpdateDTO, Vehicles>();
            #endregion

            #region Supplier
            CreateMap<Suppliers, SupplierDTO>();
            CreateMap<SupplierCreateDTO, Suppliers>();
            CreateMap<SupplierUpdateDTO, Suppliers>();
            #endregion

            #region TempImportVehicle
            CreateMap<TempVehicleImports, TemporaryVehicleDTO>();
            CreateMap<TemporaryVehicleCreateDTO, TempVehicleImports>();
            CreateMap<TemporaryVehicleUpdateDTO, TempVehicleImports>();
            #endregion

            #region Import Fitment
            CreateMap<ImportFitment, ImportFitmentDTO>();
            CreateMap<ImportFitmentCreateDTO, ImportFitment>();
            CreateMap<ImportFitmentUpdateDTO, ImportFitment>();
            #endregion
        }

    }
}
