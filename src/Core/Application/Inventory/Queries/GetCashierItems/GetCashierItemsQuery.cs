using System.Collections.Generic;
using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.DTOs;
using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Queries.GetCashierItems;

public record GetCashierItemsQuery(
    string ShopId,
    string? SearchTerm = null,
    string? Category = null,
    int Page = 1,
    int Limit = 50) : IRequest<PagedResult<CashierItemDto>>;

public class GetCashierItemsQueryHandler : IRequestHandler<GetCashierItemsQuery, PagedResult<CashierItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetCashierItemsQueryHandler(
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        ICategoryRepository categoryRepository)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResult<CashierItemDto>> Handle(GetCashierItemsQuery request, CancellationToken cancellationToken)
    {
        // Get all inventory for the shop (no pagination at repo level)
        var allInventory = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);

        // Filter to available items only
        var availableInventory = allInventory.Where(inv => inv.TotalStock > 0).ToList();

        // Get drug IDs
        var drugIds = availableInventory.Select(inv => inv.DrugId).Distinct().ToList();

        // Get all drugs info (using pagination with large limit)
        var allDrugsResult = await _drugRepository.GetAllAsync(1, 10000, cancellationToken);
        var drugsDict = allDrugsResult.Data
            .Where(d => drugIds.Contains(d.Id))
            .ToDictionary(d => d.Id);

        // Get all categories for logo/color lookup
        var allCategories = await _categoryRepository.GetAllAsync(true, cancellationToken);
        var categoriesById = allCategories.ToDictionary(c => c.CategoryId, StringComparer.OrdinalIgnoreCase);
        var categoriesByName = allCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Apply search and category filters
        IEnumerable<ShopInventory> query = availableInventory;

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(inv =>
                drugsDict.ContainsKey(inv.DrugId) && (
                    drugsDict[inv.DrugId].BrandName.ToLower().Contains(searchLower) ||
                    drugsDict[inv.DrugId].GenericName.ToLower().Contains(searchLower) ||
                    drugsDict[inv.DrugId].Barcode.ToLower().Contains(searchLower) ||
                    (drugsDict[inv.DrugId].Category?.Name ?? drugsDict[inv.DrugId].CategoryName).ToLower().Contains(searchLower)
                ));
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(inv =>
                drugsDict.ContainsKey(inv.DrugId) &&
                (drugsDict[inv.DrugId].CategoryId.Equals(request.Category, StringComparison.OrdinalIgnoreCase) ||
                 (drugsDict[inv.DrugId].Category?.Name ?? drugsDict[inv.DrugId].CategoryName).Equals(request.Category, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredInventory = query.ToList();
        var totalCount = filteredInventory.Count;

        // Apply pagination
        var pagedInventory = filteredInventory
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList();

        // Map to cashier DTOs
        var items = pagedInventory.Select(inv =>
        {
            var drug = drugsDict.GetValueOrDefault(inv.DrugId);
            if (drug == null) return null;

            var oldestBatch = inv.Batches.OrderBy(b => b.ExpiryDate).FirstOrDefault();
            var unitPrice = inv.ShopPricing?.SellingPrice ?? drug.BasePricing.SuggestedRetailPrice;
            var discount = inv.ShopPricing?.Discount ?? 0;
            var finalPrice = unitPrice * (1 - discount / 100);

            // Get category details (logo and color)
            var categoryInfo = categoriesById.GetValueOrDefault(drug.CategoryId);
            categoryInfo ??= categoriesByName.GetValueOrDefault(drug.Category?.Name ?? string.Empty);

            return new CashierItemDto
            {
                DrugId = drug.Id,
                BrandName = drug.BrandName,
                GenericName = drug.GenericName,
                Barcode = drug.Barcode,
                CategoryId = drug.CategoryId,
                Category = drug.Category?.Name ?? string.Empty,
                CategoryLogoUrl = categoryInfo?.LogoUrl, // Category logo
                CategoryColorCode = categoryInfo?.ColorCode, // Category color
                Manufacturer = drug.Manufacturer,
                ImageUrls = drug.ImageUrls,

                AvailableStock = inv.TotalStock,
                IsAvailable = inv.TotalStock > 0,
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
        })
        .Where(dto => dto != null)
        .Cast<CashierItemDto>()
        .ToList();

        return new PagedResult<CashierItemDto>
        {
            Data = items,
            Total = totalCount,
            Page = request.Page,
            Limit = request.Limit
        };
    }
}
