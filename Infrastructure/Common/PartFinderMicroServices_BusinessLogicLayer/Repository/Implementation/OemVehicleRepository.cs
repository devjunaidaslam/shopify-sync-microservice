using Microsoft.EntityFrameworkCore;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation
{
    public class OemVehicleRepository : IOemVehicleRepository
    {
        private readonly PartFinderDbContext _context;

        public OemVehicleRepository(PartFinderDbContext context)
        {
            _context = context;
        }

        public async Task<OemVehicle> AttachAsync(int oemId, long vehicleId, long? supplierId)
        {
            // Validate OEM
            var oem = await _context.OEMs.FirstOrDefaultAsync(x => x.Id == oemId)
                ?? throw new InvalidOperationException($"OEM with id {oemId} not found");

            // Validate Vehicle
            var vehicleExists = await _context.Vehicles.AnyAsync(x => x.VehicleId == vehicleId);
            if (!vehicleExists)
                throw new InvalidOperationException($"Vehicle with id {vehicleId} not found");

            var existing = await _context.OemVehicles
                .FirstOrDefaultAsync(x => x.OEMId == oemId && x.VehicleId == vehicleId);

            if (existing != null)
            {
                existing.SupplierId = supplierId;
                existing.OEM = oem.Name; // keep OEM name in sync
                existing.DeletedAt = null; // restore if soft-deleted
                await _context.SaveChangesAsync();
                return existing;
            }

            var link = new OemVehicle
            {
                OEMId = oemId,
                VehicleId = vehicleId,
                SupplierId = supplierId,
                OEM = oem.Name,
                DeletedAt = null
            };

            _context.OemVehicles.Add(link);
            await _context.SaveChangesAsync();
            return link;
        }

        public async Task<bool> DetachAsync(int oemId, long vehicleId)
        {
            var existing = await _context.OemVehicles
                .FirstOrDefaultAsync(x => x.OEMId == oemId && x.VehicleId == vehicleId && x.DeletedAt == null);

            if (existing == null)
                return false;

            existing.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int oemId, long vehicleId)
        {
            return await _context.OemVehicles.AnyAsync(x => x.OEMId == oemId && x.VehicleId == vehicleId && x.DeletedAt == null);
        }
    }
}
