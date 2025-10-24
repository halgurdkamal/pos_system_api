using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetCashierItemByBarcode;

public record GetCashierItemByBarcodeQuery(
    string ShopId,
    string Barcode) : IRequest<CashierItemDto?>;

public class GetCashierItemByBarcodeQueryHandler : IRequestHandler<GetCashierItemByBarcodeQuery, CashierItemDto?>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetCashierItemByBarcodeQueryHandler(
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        ICategoryRepository categoryRepository)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<CashierItemDto?> Handle(GetCashierItemByBarcodeQuery request, CancellationToken cancellationToken)
    {
        // Find drug by barcode
        var drug = await _drugRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
        
        if (drug == null)
        {
            return null;
        }
        
        // Get inventory for this drug in the shop
        var shopInventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId, 
            drug.Id, 
            cancellationToken);
        
        if (shopInventory == null || shopInventory.TotalStock <= 0)
        {
            return null;
        }
        
        // Get category details (logo and color)
        var categoryInfo = await _categoryRepository.GetByNameAsync(drug.Category, cancellationToken);
        
        // Get oldest batch info
        var oldestBatch = shopInventory.Batches.OrderBy(b => b.ExpiryDate).FirstOrDefault();
        
        // Calculate pricing
        var unitPrice = shopInventory.ShopPricing?.SellingPrice ?? drug.BasePricing.SuggestedRetailPrice;
        var discount = shopInventory.ShopPricing?.Discount ?? 0;
        var finalPrice = unitPrice * (1 - discount / 100);
        
        return new CashierItemDto
        {
            DrugId = drug.Id,
            BrandName = drug.BrandName,
            GenericName = drug.GenericName,
            Barcode = drug.Barcode,
            Category = drug.Category,
            CategoryLogoUrl = categoryInfo?.LogoUrl, // Category logo
            CategoryColorCode = categoryInfo?.ColorCode, // Category color
            Manufacturer = drug.Manufacturer,
            ImageUrls = drug.ImageUrls,
            
            AvailableStock = shopInventory.TotalStock,
            IsAvailable = shopInventory.TotalStock > 0,
            OldestBatchNumber = oldestBatch?.BatchNumber,
            NearestExpiryDate = oldestBatch?.ExpiryDate,
            
            UnitPrice = unitPrice,
            DiscountPercentage = discount > 0 ? discount : null,
            FinalPrice = finalPrice,
            
            Strength = drug.Formulation.Strength,
            Form = drug.Formulation.Form,
            PackageSize = $"{drug.Formulation.Form} - {drug.Formulation.Strength}", // Combine form and strength
            
            RequiresPrescription = drug.Regulatory.IsPrescriptionRequired,
            QuickNotes = drug.Description
        };
    }
}
