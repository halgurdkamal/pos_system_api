using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Drugs.DTOs;
using Microsoft.EntityFrameworkCore;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrugDetail;

public record GetDrugDetailQuery(string DrugId) : IRequest<DrugDetailDto?>;

public class GetDrugDetailQueryHandler : IRequestHandler<GetDrugDetailQuery, DrugDetailDto?>
{
    private readonly IDrugRepository _drugRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IShopRepository _shopRepository;
    private readonly ApplicationDbContext _context;

    public GetDrugDetailQueryHandler(
        IDrugRepository drugRepository,
        ICategoryRepository categoryRepository,
        IShopRepository shopRepository,
        ApplicationDbContext context)
    {
        _drugRepository = drugRepository;
        _categoryRepository = categoryRepository;
        _shopRepository = shopRepository;
        _context = context;
    }

    public async Task<DrugDetailDto?> Handle(GetDrugDetailQuery request, CancellationToken cancellationToken)
    {
        // Get drug
        var drug = await _drugRepository.GetByIdAsync(request.DrugId, cancellationToken);
        if (drug == null)
            return null;

        // Get category info
        var categoryInfo = await _categoryRepository.GetByIdAsync(drug.CategoryId, cancellationToken);

        // Get ALL shop inventories for this drug
        var shopInventories = await _context.ShopInventory
            .AsNoTracking()
            .Where(inv => inv.DrugId == request.DrugId)
            .ToListAsync(cancellationToken);

        // Get shop details for inventory
        var shopIds = shopInventories.Select(inv => inv.ShopId).Distinct().ToList();
        var shops = new Dictionary<string, Core.Domain.Shops.Entities.Shop>();
        
        foreach (var shopId in shopIds)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop != null)
                shops[shopId] = shop;
        }

        // Map shop inventories
        var shopInventorySummaries = shopInventories
            .Where(inv => shops.ContainsKey(inv.ShopId))
            .Select(inv =>
            {
                var shop = shops[inv.ShopId];
                var sellingPrice = inv.ShopPricing?.SellingPrice ?? drug.BasePricing.SuggestedRetailPrice;
                var discount = inv.ShopPricing?.Discount ?? 0;
                var finalPrice = sellingPrice * (1 - discount / 100);
                var nearestExpiry = inv.Batches.Any() ? inv.Batches.Min(b => b.ExpiryDate) : (DateTime?)null;

                return new ShopInventorySummaryDto
                {
                    ShopId = shop.Id,
                    ShopName = shop.ShopName,
                    ShopAddress = $"{shop.Address.Street}, {shop.Address.City}",
                    Quantity = inv.TotalStock,
                    SellingPrice = sellingPrice,
                    DiscountPercentage = discount > 0 ? discount : null,
                    FinalPrice = finalPrice,
                    BatchCount = inv.Batches.Count,
                    NearestExpiryDate = nearestExpiry,
                    IsLowStock = inv.TotalStock <= inv.ReorderPoint,
                    IsAvailable = inv.TotalStock > 0
                };
            })
            .OrderByDescending(s => s.Quantity)
            .ToList();

        // Calculate overall statistics
        var totalStock = shopInventorySummaries.Sum(s => s.Quantity);
        var shopsWithStock = shopInventorySummaries.Count(s => s.IsAvailable);
        var prices = shopInventorySummaries.Where(s => s.IsAvailable).Select(s => s.FinalPrice).ToList();
        var avgPrice = prices.Any() ? prices.Average() : 0;
        var lowestPrice = prices.Any() ? prices.Min() : 0;
        var highestPrice = prices.Any() ? prices.Max() : 0;

        return new DrugDetailDto
        {
            DrugId = drug.DrugId,
            Barcode = drug.Barcode,
            BarcodeType = drug.BarcodeType,
            BrandName = drug.BrandName,
            GenericName = drug.GenericName,
            Manufacturer = drug.Manufacturer,
            OriginCountry = drug.OriginCountry,
            CategoryId = drug.CategoryId,
            Category = categoryInfo?.Name ?? string.Empty,
            CategoryLogoUrl = categoryInfo?.LogoUrl,
            CategoryColorCode = categoryInfo?.ColorCode,
            ImageUrls = drug.ImageUrls,
            Description = drug.Description,
            SideEffects = drug.SideEffects,
            InteractionNotes = drug.InteractionNotes,
            Tags = drug.Tags,
            RelatedDrugs = drug.RelatedDrugs,

            Formulation = new FormulationDto
            {
                Strength = drug.Formulation.Strength,
                Form = drug.Formulation.Form,
                RouteOfAdministration = drug.Formulation.RouteOfAdministration,
                ActiveIngredients = "" // Not in Formulation value object
            },

            Pricing = new PricingDto
            {
                SuggestedRetailPrice = drug.BasePricing.SuggestedRetailPrice,
                WholesalePrice = 0, // Not available in BasePricing
                ManufacturerPrice = 0 // Not available in BasePricing
            },

            Regulatory = new RegulatoryDto
            {
                IsPrescriptionRequired = drug.Regulatory.IsPrescriptionRequired,
                ControlledSubstanceSchedule = null, // Not available in Regulatory
                FdaApprovalNumber = null // Not available in Regulatory
            },

            ShopInventories = shopInventorySummaries,
            TotalQuantityInStock = totalStock,
            TotalShopsWithStock = shopsWithStock,
            AveragePriceAcrossShops = avgPrice,
            LowestPriceAvailable = lowestPrice,
            HighestPriceAvailable = highestPrice,

            CreatedAt = drug.CreatedAt,
            LastUpdated = drug.LastUpdated
        };
    }
}
