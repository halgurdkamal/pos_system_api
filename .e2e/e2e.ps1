$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5135'
$RUN  = (Get-Date -Format 'yyyyMMddHHmmss')

function Step($n, $msg) { Write-Host ("`n[" + $n + "] " + $msg) -ForegroundColor Cyan }
function Ok($msg)       { Write-Host ("  OK   " + $msg) -ForegroundColor Green }
function Fail($msg)     { Write-Host ("  FAIL " + $msg) -ForegroundColor Red }
function Info($msg)     { Write-Host ("  ..   " + $msg) -ForegroundColor Gray }

$results = @()

function Try-Step {
    param([string]$Name, [scriptblock]$Action)
    try {
        & $Action
        $script:results += [pscustomobject]@{ Step = $Name; Status = 'PASS'; Detail = '' }
    } catch {
        $resp = $null
        try {
            if ($_.Exception.Response) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $resp = $reader.ReadToEnd()
            }
        } catch {}
        Fail ("$Name -> " + $_.Exception.Message)
        if ($resp) { Fail ("       body: " + $resp) }
        $script:results += [pscustomobject]@{ Step = $Name; Status = 'FAIL'; Detail = ($_.Exception.Message + ' | ' + $resp) }
    }
}

# ---------------- 1. Login ----------------
Step 1 'Login as bootadmin'
$loginBody = @{ identifier = 'bootadmin@test.local'; password = 'Boot@dminPass1!' } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
$TOKEN = $login.accessToken
$HDR = @{ Authorization = "Bearer $TOKEN" }
$SHOP_ID = $login.user.shops[0].shopId
$USER_ID = $login.user.id
Ok ("token len=" + $TOKEN.Length + " systemRole=" + $login.user.systemRole + " shop=" + $SHOP_ID)
$results += [pscustomobject]@{ Step = '1. Login'; Status = 'PASS'; Detail = "shop=$SHOP_ID" }

# ---------------- 2. /me ----------------
Step 2 '/api/auth/me'
Try-Step 'auth/me' {
    $me = Invoke-RestMethod -Uri "$base/api/auth/me" -Headers $HDR
    Ok ("user=" + $me.username + " role=" + $me.systemRole + " shops=" + $me.shops.Count)
}

# ---------------- 3. Categories: list / create ----------------
Step 3 'Categories list / create'
$category = $null
Try-Step 'categories.list' {
    $cats = Invoke-RestMethod -Uri "$base/api/categories?activeOnly=true" -Headers $HDR
    Ok ("existing categories=" + $cats.Count)
    if ($cats.Count -gt 0) {
        $script:category = $cats[0]
        Info ("reusing category " + $cats[0].categoryId + ' ' + $cats[0].name)
    }
}
if (-not $category) {
    Try-Step 'categories.create' {
        $body = @{
            name         = "E2E-Antibiotics-$(Get-Random -Minimum 1000 -Maximum 9999)"
            logoUrl      = $null
            description  = "E2E test category"
            colorCode    = "#FF5733"
            displayOrder = 1
        } | ConvertTo-Json
        $script:category = Invoke-RestMethod -Uri "$base/api/categories" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
        Ok ("created " + $script:category.categoryId + ' ' + $script:category.name)
    }
}
$CAT_ID = $category.categoryId

# ---------------- 4. Drug create ----------------
Step 4 'Drug create'
$DRUG_ID = $null
$BARCODE = "E2E$([guid]::NewGuid().ToString('N').Substring(0,9))"
$drug = $null
Try-Step 'drugs.create' {
    $body = @{
        brandName     = "E2E Amoxil 500"
        genericName   = "Amoxicillin (e2e)"
        manufacturer  = "GSK"
        originCountry = "UK"
        categoryId    = $CAT_ID
        barcode       = $BARCODE
        barcodeType   = "EAN-13"
        imageUrls     = @()
        description   = "E2E test drug"
        sideEffects   = @("Nausea")
        tags          = @("e2e","antibiotic")
        formulation   = @{
            form = "Capsule"; strength = "500mg"; routeOfAdministration = "Oral"
        }
        basePricing   = @{
            suggestedRetailPrice = 5.99; currency = "USD"; suggestedTaxRate = 0.10
        }
        regulatory    = @{
            isPrescriptionRequired = $true; isHighRisk = $false
            drugAuthorityNumber    = "FDA-E2E"
            approvalDate           = "2020-01-15T00:00:00Z"
            controlSchedule        = "Schedule IV"
        }
        packagingInfo = @{
            unitType            = "Count"
            baseUnit            = "tablet"
            baseUnitDisplayName = "Tablet"
            isSubdivisible      = $true
            packagingLevels     = @(
                @{ levelNumber=1; unitName="Tablet"; baseUnitQuantity=1;   isSellable=$false; isDefault=$false; isBreakable=$true },
                @{ levelNumber=2; unitName="Strip";  baseUnitQuantity=10;  quantityPerParent=10; isSellable=$true;  isDefault=$false; isBreakable=$true },
                @{ levelNumber=3; unitName="Box";    baseUnitQuantity=100; quantityPerParent=10; isSellable=$true;  isDefault=$true;  isBreakable=$false; minimumSaleQuantity=1 }
            )
        }
    } | ConvertTo-Json -Depth 10
    $script:drug = Invoke-RestMethod -Uri "$base/api/drugs" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    Ok ("drug " + $script:drug.drugId + ' "' + $script:drug.brandName + '" barcode=' + $script:drug.barcode)
}
if ($drug) { $DRUG_ID = $drug.drugId }

# ---------------- 5. Supplier ----------------
Step 5 'Supplier create'
$SUPPLIER_ID = $null
$supplier = $null
Try-Step 'suppliers.create' {
    $body = @{
        supplierName     = "E2E Alpha Pharma $RUN"
        supplierType     = "Distributor"
        contactNumber    = "+9647901234567"
        email            = "e2e+$RUN@alpha.test"
        address          = @{ street="123 Medical St"; city="Baghdad"; country="Iraq" }
        paymentTerms     = "Net 30"
        deliveryLeadTime = 7
        minimumOrderValue= 0
        taxId            = "IRQ-E2E-$RUN"
        licenseNumber    = "DIST-E2E-$RUN"
    } | ConvertTo-Json
    $script:supplier = Invoke-RestMethod -Uri "$base/api/suppliers" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    Ok ("supplier " + $script:supplier.id)
}
if ($supplier) { $SUPPLIER_ID = $supplier.id }

# ---------------- 6. Prime inventory (workaround for F-1) ----------------
Step 6 'Prime ShopInventory with initial batch (avoids F-1 first-receive crash)'
$primedInv = $null
Try-Step 'inventory.addStock (prime)' {
    if (-not $SUPPLIER_ID) { throw "SUPPLIER_ID is null - upstream supplier step failed" }
    $body = @{
        drugId          = $DRUG_ID
        supplierId      = $SUPPLIER_ID
        batchNumber     = "BATCH-E2E-PRIME-$RUN"
        quantity        = 50
        expiryDate      = "2028-05-01T00:00:00Z"
        purchasePrice   = 5.50
        sellingPrice    = 12.00
        storageLocation = "Shelf A-3"
        reorderPoint    = 10
    } | ConvertTo-Json
    $script:primedInv = Invoke-RestMethod -Uri "$base/api/inventory/shops/$SHOP_ID/stock" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    Ok ("inventory id=" + $script:primedInv.id + " totalStock=" + $script:primedInv.totalStock + " batches=" + $script:primedInv.batches.Count)
}

# ---------------- 7. PO lifecycle ----------------
Step 7 'Purchase order: create -> submit -> confirm -> receive'
$po = $null
$poItemId = $null
Try-Step 'po.create' {
    $body = @{
        shopId               = $SHOP_ID
        supplierId           = $SUPPLIER_ID
        priority             = "High"
        expectedDeliveryDate = "2026-05-15T00:00:00Z"
        paymentTerms         = "Net30"
        deliveryAddress      = "Main warehouse"
        referenceNumber      = "REQ-E2E-001"
        notes                = "E2E test PO"
        shippingCost         = 0
        taxAmount            = 0
        discountAmount       = 0
        items = @(
            @{ drugId = $DRUG_ID; quantity = 100; unitPrice = 5.50; discountPercentage = 0 }
        )
    } | ConvertTo-Json -Depth 5
    $script:po = Invoke-RestMethod -Uri "$base/api/purchaseorders" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    $script:poItemId = $script:po.items[0].id
    Ok ("PO " + $script:po.id + ' status=' + $script:po.status + ' total=' + $script:po.totalAmount)
}
if ($po) {
    Try-Step 'po.submit' {
        $r = Invoke-RestMethod -Uri "$base/api/purchaseorders/$($script:po.id)/submit" -Method Post -Headers $HDR
        Ok ("status=" + $r.status)
    }
    Try-Step 'po.confirm' {
        $r = Invoke-RestMethod -Uri "$base/api/purchaseorders/$($script:po.id)/confirm" -Method Post -Headers $HDR
        Ok ("status=" + $r.status)
    }
    Try-Step 'po.receive' {
        $body = @{
            items = @(
                @{ itemId = $script:poItemId; quantity = 100; batchNumber = "BATCH-E2E-PO1-$RUN"; expiryDate = "2028-06-01T00:00:00Z" }
            )
        } | ConvertTo-Json -Depth 5
        $r = Invoke-RestMethod -Uri "$base/api/purchaseorders/$($script:po.id)/receive" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
        Ok ("status=" + $r.status + ' completion%=' + $r.completionPercentage)
    }
}

# Helper: fetch this run's inventory row by drugId. The pagination wrapper field is `data`, not `items`.
function Get-InventoryRow {
    param([string]$ShopId, [string]$DrugId)
    $page = 1
    while ($true) {
        $r = Invoke-RestMethod -Uri "$base/api/inventory/shops/$ShopId`?page=$page&limit=200" -Headers $HDR
        $rows = $r.data
        if (-not $rows) { return $null }
        $hit = $rows | Where-Object { $_.drugId -eq $DrugId }
        if ($hit) { return $hit }
        if ($rows.Count -lt 200) { return $null }
        $page++; if ($page -gt 50) { return $null }
    }
}

# ---------------- 8. Inventory check after receive ----------------
Step 8 'Inventory after PO receive'
Try-Step 'inventory.list' {
    $row = Get-InventoryRow -ShopId $SHOP_ID -DrugId $DRUG_ID
    if (-not $row) { throw "drug $DRUG_ID not found in shop $SHOP_ID inventory" }
    Ok ("totalStock=" + $row.totalStock + " floor=" + $row.shopFloorStock + " storage=" + $row.storageStock + " batches=" + $row.batches.Count)
    foreach ($b in $row.batches) {
        Info ("batch " + $b.batchNumber + ' qty=' + $b.quantityOnHand + ' loc=' + $b.location + ' status=' + $b.status)
    }
    if ($row.totalStock -ne 150) { throw "expected 150 (50 prime + 100 PO), got $($row.totalStock)" }
}

# ---------------- 9. Move some stock to floor ----------------
Step 9 'Move 30 units to shop floor'
Try-Step 'inventory.move-to-floor' {
    $body = @{ quantity = 30; batchNumber = $null } | ConvertTo-Json
    $r = Invoke-RestMethod -Uri "$base/api/inventory/shops/$SHOP_ID/drugs/$DRUG_ID/move-to-floor" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    Ok ("after move floor=" + $r.shopFloorStock + " storage=" + $r.storageStock)
}

# ---------------- 10. Set per-level pricing ----------------
Step 10 'Set per-level pricing (modern model)'
Try-Step 'inventory.packaging-pricing.update-from-batch' {
    $r = Invoke-RestMethod -Uri "$base/api/inventory/shops/$SHOP_ID/drugs/$DRUG_ID/packaging-pricing/update-from-batch" -Method Post -Headers $HDR
    Ok 'auto-sync ran'
}

# ---------------- 11. POS lookup by barcode ----------------
Step 11 'POS scan-by-barcode lookup (resolves via DrugId after Bug 2 fix)'
Try-Step 'inventory.pos-items.by-barcode' {
    $r = Invoke-RestMethod -Uri "$base/api/inventory/shops/$SHOP_ID/pos-items/by-barcode/$BARCODE" -Headers $HDR
    if ($r.drugId -ne $DRUG_ID) { throw "DTO returned drugId=$($r.drugId), expected $DRUG_ID (raw GUID would indicate Bug 2 regression)" }
    if ($r.availableStock -lt 1) { throw "availableStock=$($r.availableStock); inventory should reflect 150 after PO+prime, ~120 after move-to-floor" }
    Ok ("brand=" + $r.brandName + " stock=" + $r.availableStock + " price=" + $r.unitPrice + " drugId=" + $r.drugId)
}

# ---------------- 11b. /api/barcodes/search (Bug 2 sibling) ----------------
Step '11b' '/api/barcodes/search returns DrugId not raw GUID'
Try-Step 'barcodes.search' {
    $r = Invoke-RestMethod -Uri "$base/api/barcodes/search?barcode=$BARCODE" -Headers $HDR
    if ($r.drugId -ne $DRUG_ID) { throw "drugId=$($r.drugId), expected $DRUG_ID" }
    Ok ("drugId=" + $r.drugId + " brandName=" + $r.brandName)
}

# ---------------- 12. Sales order: create -> confirm -> payment -> complete ----------------
Step 12 'Sales order full lifecycle'
$so = $null
Try-Step 'salesorders.create' {
    $body = @{
        shopId                  = $SHOP_ID
        customerName            = "E2E Customer"
        customerPhone           = "+1234567890"
        isPrescriptionRequired  = $false
        notes                   = "E2E sale"
        taxAmount               = 0
        discountAmount          = 0
        items = @(
            @{ drugId = $DRUG_ID; quantity = 1; unitPrice = 12.00; packagingLevel = "Box"; discountPercentage = 0 }
        )
    } | ConvertTo-Json -Depth 5
    $script:so = Invoke-RestMethod -Uri "$base/api/salesorders" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
    Ok ("SO " + $script:so.id + ' #' + $script:so.orderNumber + ' status=' + $script:so.status + ' total=' + $script:so.totalAmount + ' baseUnits=' + $script:so.items[0].baseUnitsConsumed)
}
if ($so) {
    Try-Step 'salesorders.confirm' {
        $r = Invoke-RestMethod -Uri "$base/api/salesorders/$($script:so.id)/confirm" -Method Post -Headers $HDR
        Ok ("status=" + $r.status)
    }
    Try-Step 'salesorders.payment' {
        $body = @{ paymentMethod = "Cash"; amountPaid = 20.00; paymentReference = $null } | ConvertTo-Json
        $r = Invoke-RestMethod -Uri "$base/api/salesorders/$($script:so.id)/payment" -Method Post -Headers $HDR -ContentType 'application/json' -Body $body
        Ok ("status=" + $r.status + ' change=' + $r.changeGiven)
    }
    Try-Step 'salesorders.complete' {
        $r = Invoke-RestMethod -Uri "$base/api/salesorders/$($script:so.id)/complete" -Method Post -Headers $HDR
        Ok ("status=" + $r.status)
    }
}

# ---------------- 13. Verify stock deducted ----------------
Step 13 'Verify FIFO stock deduction uses BaseUnitsConsumed (Bug 1 fix)'
Try-Step 'inventory.verify-deducted' {
    $row = Get-InventoryRow -ShopId $SHOP_ID -DrugId $DRUG_ID
    if (-not $row) { throw "drug $DRUG_ID not found in shop $SHOP_ID inventory" }
    $expected = 50   # 50 prime + 100 received = 150, sold 1 Box (= 100 base units), expect 50 left
    $observed = $row.totalStock
    Ok ("post-sale totalStock=$observed expected=$expected floor=$($row.shopFloorStock) storage=$($row.storageStock)")
    foreach ($b in $row.batches) {
        Info ("batch " + $b.batchNumber + ' qty=' + $b.quantityOnHand + ' loc=' + $b.location)
    }
    if ($observed -ne $expected) {
        throw "FIFO deduction wrong: expected $expected base units left, got $observed (delta=$($observed - $expected)). Pre-fix this was 149 because deduction used Quantity=1 instead of BaseUnitsConsumed=100."
    }
    Ok 'FIFO deducted exactly 100 base units (1 Box of 100 tablets)'
}

# ---------------- 14. PDF receipt ----------------
Step 14 'GET PDF receipt by orderNumber'
if ($so) {
    Try-Step 'pdf.receipt' {
        $tmp = Join-Path $env:TEMP "e2e_receipt_$($script:so.orderNumber).pdf"
        Invoke-WebRequest -Uri "$base/api/pdf/receipt/$($script:so.orderNumber)" -Headers $HDR -OutFile $tmp -ErrorAction Stop
        $bytes = [System.IO.File]::ReadAllBytes($tmp)
        $head = [System.Text.Encoding]::ASCII.GetString($bytes[0..3])
        if ($head -ne '%PDF') { throw "header is '$head' not %PDF" }
        Ok ("PDF written to " + $tmp + ' size=' + $bytes.Length + ' header=' + $head)
    }
}

# ---------------- Summary ----------------
Write-Host "`n==================== SUMMARY ====================" -ForegroundColor Yellow
$results | Format-Table Step, Status -AutoSize
$pass = ($results | Where-Object { $_.Status -eq 'PASS' }).Count
$fail = ($results | Where-Object { $_.Status -eq 'FAIL' }).Count
Write-Host ("PASS=" + $pass + "  FAIL=" + $fail) -ForegroundColor Yellow
if ($fail -gt 0) {
    Write-Host "`nFailures:" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq 'FAIL' } | ForEach-Object { Write-Host ("- " + $_.Step + ": " + $_.Detail) -ForegroundColor Red }
}
