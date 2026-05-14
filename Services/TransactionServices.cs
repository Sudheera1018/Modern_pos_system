using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Services;

public class InventoryService(
    IRepository<Product> productRepository,
    IRepository<InventoryMovement> movementRepository) : IInventoryService
{
    public async Task<InventoryOverviewViewModel> GetOverviewAsync()
    {
        var products = await productRepository.GetAllAsync(x => x.IsActive);
        var movements = await movementRepository.GetAllAsync(x => x.IsActive);
        var productItems = products
            .OrderBy(x => x.ProductName)
            .Select(x => new ProductListItemViewModel
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                Barcode = x.Barcode,
                ProductName = x.ProductName,
                CategoryName = x.CategoryName,
                SupplierName = x.SupplierName,
                SellingPrice = x.SellingPrice,
                CurrentStock = x.CurrentStock,
                ReorderLevel = x.ReorderLevel,
                IsActive = x.IsActive,
                StockStatus = x.CurrentStock <= 0 ? "Out of Stock" : x.CurrentStock <= x.ReorderLevel ? "Low Stock" : "In Stock",
                ImageUrl = string.IsNullOrWhiteSpace(x.ImageUrl) ? "/images/product-placeholder.svg" : x.ImageUrl
            })
            .ToList();

        return new InventoryOverviewViewModel
        {
            Products = productItems,
            LowStockProducts = productItems.Where(x => x.CurrentStock > 0 && x.CurrentStock <= x.ReorderLevel).ToList(),
            OutOfStockProducts = productItems.Where(x => x.CurrentStock <= 0).ToList(),
            Movements = movements
                .OrderByDescending(x => x.Date)
                .Take(50)
                .Select(x => new InventoryMovementViewModel
                {
                    ProductName = x.ProductName,
                    ProductCode = x.ProductCode,
                    MovementType = x.MovementType.ToString(),
                    Quantity = x.Quantity,
                    BeforeStock = x.BeforeStock,
                    AfterStock = x.AfterStock,
                    ReferenceType = x.ReferenceType,
                    ReferenceNo = x.ReferenceNo,
                    Remarks = x.Remarks,
                    Date = x.Date,
                    UserName = x.UserName
                }).ToList()
        };
    }

    public async Task<StockAdjustmentViewModel> GetAdjustmentFormAsync() =>
        new()
        {
            Products = (await productRepository.GetAllAsync(x => x.IsActive))
                .OrderBy(x => x.ProductName)
                .Select(x => new LookupOptionViewModel { Id = x.Id, Name = $"{x.ProductName} ({x.CurrentStock} {x.Unit})" })
                .ToList()
        };

    public async Task<ServiceResult> SaveAdjustmentAsync(StockAdjustmentViewModel model, ICurrentUserContext currentUser)
    {
        var product = await productRepository.GetByIdAsync(model.ProductId);
        if (product is null)
        {
            return ServiceResult.Failure("Product not found.");
        }

        var beforeStock = product.CurrentStock;
        var delta = model.AdjustmentType == "Increase" ? model.Quantity : -model.Quantity;
        var afterStock = beforeStock + delta;
        if (afterStock < 0)
        {
            return ServiceResult.Failure("Adjustment would make stock negative.");
        }

        product.CurrentStock = afterStock;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = currentUser.UserName;
        await productRepository.UpdateAsync(product);

        await RecordMovementAsync(new InventoryMovement
        {
            ProductId = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            MovementType = model.AdjustmentType == "Increase" ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut,
            Quantity = model.Quantity,
            BeforeStock = beforeStock,
            AfterStock = afterStock,
            ReferenceType = "Adjustment",
            ReferenceNo = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Remarks = model.Remarks,
            UserId = currentUser.UserId,
            UserName = currentUser.FullName,
            CreatedBy = currentUser.UserName
        });

        return ServiceResult.Success("Stock adjustment saved successfully.");
    }

    public async Task RecordMovementAsync(InventoryMovement movement)
    {
        movement.CreatedAt = DateTime.UtcNow;
        movement.Date = DateTime.UtcNow;
        await movementRepository.AddAsync(movement);
    }
}

public class SalesService(IRepository<Sale> saleRepository) : ISalesService
{
    public async Task<string> GetNextInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}";
        var existing = await saleRepository.GetAllAsync(x => x.InvoiceNo.StartsWith(prefix));
        return $"{prefix}-{existing.Count + 1:0000}";
    }

    public Task<string> GetNextGrnNumberAsync() =>
        Task.FromResult($"GRN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}");

    public async Task<SalesHistoryPageViewModel> GetHistoryAsync(string? invoiceNo, DateTime? fromDate, DateTime? toDate, string? cashierUserId)
    {
        var sales = await saleRepository.GetAllAsync(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(invoiceNo))
        {
            sales = sales.Where(x => x.InvoiceNo.Contains(invoiceNo, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (fromDate.HasValue)
        {
            sales = sales.Where(x => x.SaleDate.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            sales = sales.Where(x => x.SaleDate.Date <= toDate.Value.Date).ToList();
        }

        if (!string.IsNullOrWhiteSpace(cashierUserId))
        {
            sales = sales.Where(x => x.CashierUserId == cashierUserId).ToList();
        }

        return new SalesHistoryPageViewModel
        {
            InvoiceNo = invoiceNo ?? string.Empty,
            FromDate = fromDate,
            ToDate = toDate,
            CashierUserId = cashierUserId,
            Sales = sales.OrderByDescending(x => x.SaleDate).Select(x => new SalesHistoryItemViewModel
            {
                Id = x.Id,
                InvoiceNo = x.InvoiceNo,
                SaleDate = x.SaleDate,
                CustomerName = x.CustomerName,
                CashierName = x.CashierName,
                PaymentMethod = FormatPaymentMethod(x.PaymentMethod),
                GrandTotal = x.NetPayable > 0 ? x.NetPayable : x.GrandTotal
            }).ToList()
        };
    }

    public async Task<InvoiceViewModel?> GetInvoiceAsync(string id)
    {
        var sale = await saleRepository.GetByIdAsync(id);
        if (sale is null)
        {
            return null;
        }

        return new InvoiceViewModel
        {
            InvoiceNo = sale.InvoiceNo,
            SaleDate = sale.SaleDate,
            CustomerName = sale.CustomerName,
            CashierName = sale.CashierName,
            PaymentMethod = FormatPaymentMethod(sale.PaymentMethod),
            Subtotal = sale.Subtotal,
            DiscountTotal = sale.DiscountTotal,
            TaxAmount = sale.TaxAmount,
            GrandTotal = sale.GrandTotal,
            LoyaltyPointsRedeemed = sale.LoyaltyPointsRedeemed,
            LoyaltyAmountRedeemed = sale.LoyaltyAmountRedeemed,
            LoyaltyPointsEarned = sale.LoyaltyPointsEarned,
            NetPayable = sale.NetPayable > 0 ? sale.NetPayable : sale.GrandTotal,
            AmountReceived = sale.AmountReceived,
            CashPaidAmount = sale.CashPaidAmount,
            CardPaidAmount = sale.CardPaidAmount,
            FundTransferAmount = sale.FundTransferAmount,
            CardReference = sale.CardReference,
            TransferReferenceNo = sale.TransferReferenceNo,
            TransferProvider = sale.TransferProvider,
            BalanceReturn = sale.BalanceReturn,
            Items = sale.Items.Select(x => new InvoiceItemViewModel
            {
                ProductName = x.ProductName,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                DiscountAmount = x.DiscountAmount,
                LineTotal = x.LineTotal
            }).ToList()
        };
    }

    public async Task<List<Sale>> GetSalesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var sales = await saleRepository.GetAllAsync(x => x.IsActive);
        if (fromDate.HasValue)
        {
            sales = sales.Where(x => x.SaleDate.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            sales = sales.Where(x => x.SaleDate.Date <= toDate.Value.Date).ToList();
        }

        return sales;
    }

    private static string FormatPaymentMethod(PaymentMethod paymentMethod) => paymentMethod switch
    {
        PaymentMethod.FundTransfer => "Fund Transfer",
        PaymentMethod.Split => "Split Payment",
        _ => paymentMethod.ToString()
    };
}

public class PosService(
    IProductService productService,
    ICustomerService customerService,
    IRepository<Customer> customerRepository,
    IRepository<Sale> saleRepository,
    IRepository<Product> productRepository,
    IInventoryService inventoryService,
    ISalesService salesService,
    ILoyaltyPolicyService loyaltyPolicyService) : IPosService
{
    public async Task<PosPageViewModel> GetPageAsync(ICurrentUserContext currentUser)
    {
        var loyaltyPolicy = await loyaltyPolicyService.GetPolicyAsync();
        return new PosPageViewModel
        {
            CashierName = currentUser.FullName,
            RoleName = currentUser.RoleName,
            PreviewInvoiceNo = await salesService.GetNextInvoiceNumberAsync(),
            WalkInCustomerId = await customerService.GetWalkInCustomerIdAsync(),
            Customers = await customerService.GetPosLookupAsync(),
            Products = await productService.SearchForPosAsync(),
            LoyaltyEarnRateAmount = loyaltyPolicy.EarnRateAmount,
            LoyaltyRedeemValue = loyaltyPolicy.RedeemValue,
            MinimumRedeemPoints = loyaltyPolicy.MinimumRedeemPoints,
            PaymentMethods =
            [
                new() { Value = nameof(PaymentMethod.Cash), Label = "Cash" },
                new() { Value = nameof(PaymentMethod.Card), Label = "Card" },
                new() { Value = nameof(PaymentMethod.FundTransfer), Label = "Fund Transfer" },
                new() { Value = nameof(PaymentMethod.Split), Label = "Split Payment" }
            ]
        };
    }

    public async Task<ServiceResult<string>> CompleteCheckoutAsync(CheckoutRequestDto model, ICurrentUserContext currentUser)
    {
        if (model.Items.Count == 0)
        {
            return ServiceResult<string>.Failure("Please add at least one item.");
        }

        var productIds = model.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds);
        if (products.Count != productIds.Count)
        {
            return ServiceResult<string>.Failure("One or more products could not be found.");
        }

        decimal subtotal = 0;
        decimal discountTotal = 0;
        var saleItems = new List<SaleItem>();

        foreach (var item in model.Items)
        {
            var product = products.First(x => x.Id == item.ProductId);
            if (product.CurrentStock < item.Quantity)
            {
                return ServiceResult<string>.Failure($"Insufficient stock for {product.ProductName}.");
            }

            var lineTotal = (product.SellingPrice * item.Quantity) - item.DiscountAmount;
            saleItems.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Barcode = product.Barcode,
                Quantity = item.Quantity,
                UnitPrice = product.SellingPrice,
                DiscountAmount = item.DiscountAmount,
                LineTotal = lineTotal
            });
            subtotal += product.SellingPrice * item.Quantity;
            discountTotal += item.DiscountAmount;
        }

        var customer = await customerService.GetByIdAsync(model.CustomerId);
        if (customer is null)
        {
            return ServiceResult<string>.Failure("Selected customer could not be found.");
        }

        if (model.IsWalkInCustomer && customer.Id != await customerService.GetWalkInCustomerIdAsync())
        {
            model.IsWalkInCustomer = false;
        }

        var loyaltyPolicy = await loyaltyPolicyService.GetPolicyAsync();
        var grandTotal = subtotal - discountTotal + model.TaxAmount;
        var availablePoints = model.IsWalkInCustomer ? 0 : customer.LoyaltyPoints;
        var requestedPoints = model.UseMaxLoyaltyPoints
            ? availablePoints
            : Math.Max(0, decimal.Truncate(model.RedeemedLoyaltyPoints));

        if (model.IsWalkInCustomer && requestedPoints > 0)
        {
            return ServiceResult<string>.Failure("Walk-in customers cannot redeem loyalty points.");
        }

        if (!model.IsWalkInCustomer && requestedPoints > 0 && requestedPoints < loyaltyPolicy.MinimumRedeemPoints)
        {
            return ServiceResult<string>.Failure($"A minimum of {loyaltyPolicy.MinimumRedeemPoints:N0} points is required for redemption.");
        }

        if (requestedPoints > availablePoints)
        {
            return ServiceResult<string>.Failure("Redeemed loyalty points exceed the customer's available balance.");
        }

        var maxRedeemablePoints = loyaltyPolicy.RedeemValue <= 0
            ? 0
            : Math.Floor(grandTotal / loyaltyPolicy.RedeemValue);
        var loyaltyPointsRedeemed = Math.Min(requestedPoints, maxRedeemablePoints);
        var loyaltyAmountRedeemed = loyaltyPointsRedeemed * loyaltyPolicy.RedeemValue;
        var netPayable = Math.Max(0, grandTotal - loyaltyAmountRedeemed);

        var payment = model.Payment ?? new CheckoutPaymentBreakdownDto();
        var paidAmount = payment.CashAmount + payment.CardAmount + payment.FundTransferAmount;

        switch (model.PaymentMethod)
        {
            case PaymentMethod.Cash when payment.CashAmount < netPayable:
                return ServiceResult<string>.Failure("Cash received is less than the payable total.");
            case PaymentMethod.Cash when payment.CardAmount > 0 || payment.FundTransferAmount > 0:
                return ServiceResult<string>.Failure("Cash checkout should only use the cash amount field.");
            case PaymentMethod.Card when payment.CardAmount < netPayable:
                return ServiceResult<string>.Failure("Card payment is less than the payable total.");
            case PaymentMethod.Card when payment.CashAmount > 0 || payment.FundTransferAmount > 0:
                return ServiceResult<string>.Failure("Card checkout should only use the card amount field.");
            case PaymentMethod.FundTransfer when payment.FundTransferAmount < netPayable:
                return ServiceResult<string>.Failure("Fund transfer amount is less than the payable total.");
            case PaymentMethod.FundTransfer when string.IsNullOrWhiteSpace(payment.TransferReferenceNo):
                return ServiceResult<string>.Failure("Fund transfer reference number is required.");
            case PaymentMethod.FundTransfer when payment.CashAmount > 0 || payment.CardAmount > 0:
                return ServiceResult<string>.Failure("Fund transfer checkout should only use the transfer amount field.");
            case PaymentMethod.Split when paidAmount < netPayable:
                return ServiceResult<string>.Failure("Combined split payment is less than the payable total.");
        }

        if (paidAmount < netPayable)
        {
            return ServiceResult<string>.Failure("Received payment is less than the payable total.");
        }

        var loyaltyPointsEarned = model.IsWalkInCustomer || loyaltyPolicy.EarnRateAmount <= 0
            ? 0
            : Math.Floor(netPayable / loyaltyPolicy.EarnRateAmount);

        var invoiceNo = await salesService.GetNextInvoiceNumberAsync();
        var sale = new Sale
        {
            InvoiceNo = invoiceNo,
            SaleDate = DateTime.UtcNow,
            CustomerId = model.CustomerId,
            CustomerName = $"{customer.FullName} ({customer.CustomerCode})",
            IsWalkInCustomer = model.IsWalkInCustomer,
            CashierUserId = currentUser.UserId,
            CashierName = currentUser.FullName,
            PaymentMethod = model.PaymentMethod,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            TaxAmount = model.TaxAmount,
            GrandTotal = grandTotal,
            LoyaltyPointsRedeemed = loyaltyPointsRedeemed,
            LoyaltyAmountRedeemed = loyaltyAmountRedeemed,
            LoyaltyPointsEarned = loyaltyPointsEarned,
            NetPayable = netPayable,
            AmountReceived = paidAmount,
            CashPaidAmount = payment.CashAmount,
            CardPaidAmount = payment.CardAmount,
            FundTransferAmount = payment.FundTransferAmount,
            CardReference = payment.CardReference.Trim(),
            TransferReferenceNo = payment.TransferReferenceNo.Trim(),
            TransferProvider = payment.TransferProvider.Trim(),
            BalanceReturn = paidAmount - netPayable,
            Notes = model.Notes,
            Items = saleItems,
            CreatedBy = currentUser.UserName
        };

        await saleRepository.AddAsync(sale);

        foreach (var item in saleItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var beforeStock = product.CurrentStock;
            product.CurrentStock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = currentUser.UserName;
            await productRepository.UpdateAsync(product);

            await inventoryService.RecordMovementAsync(new InventoryMovement
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                MovementType = StockMovementType.Sale,
                Quantity = item.Quantity,
                BeforeStock = beforeStock,
                AfterStock = product.CurrentStock,
                ReferenceType = "Sale",
                ReferenceNo = invoiceNo,
                Remarks = "Stock reduced after POS checkout.",
                UserId = currentUser.UserId,
                UserName = currentUser.FullName,
                CreatedBy = currentUser.UserName
            });
        }

        if (!model.IsWalkInCustomer)
        {
            customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - loyaltyPointsRedeemed + loyaltyPointsEarned);
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = currentUser.UserName;
            await customerRepository.UpdateAsync(customer);
        }

        return ServiceResult<string>.Success(sale.Id, $"Sale completed successfully. Invoice {invoiceNo} created.");
    }
}

public class PurchaseService(
    IRepository<Purchase> purchaseRepository,
    IRepository<Product> productRepository,
    ISupplierService supplierService,
    IProductService productService,
    IInventoryService inventoryService,
    ISalesService salesService) : IPurchaseService
{
    public async Task<PurchasePageViewModel> GetPageAsync()
    {
        var purchases = await purchaseRepository.GetAllAsync(x => x.IsActive);
        return new PurchasePageViewModel
        {
            Form = new PurchaseFormViewModel
            {
                GrnNumber = await salesService.GetNextGrnNumberAsync(),
                Suppliers = await supplierService.GetLookupAsync(),
                Products = await productService.GetLookupAsync(),
                Items = [new PurchaseItemFormViewModel()]
            },
            Purchases = purchases.OrderByDescending(x => x.GrnDate).Select(x => new PurchaseListItemViewModel
            {
                Id = x.Id,
                GrnNumber = x.GrnNumber,
                SupplierName = x.SupplierName,
                GrnDate = x.GrnDate,
                TotalAmount = x.TotalAmount,
                ReceivedByName = x.ReceivedByName
            }).ToList()
        };
    }

    public async Task<ServiceResult> SaveAsync(PurchaseFormViewModel model, ICurrentUserContext currentUser)
    {
        var supplier = (await supplierService.GetLookupAsync()).FirstOrDefault(x => x.Id == model.SupplierId);
        if (supplier is null)
        {
            return ServiceResult.Failure("Please select a supplier.");
        }

        var itemRows = model.Items.Where(x => !string.IsNullOrWhiteSpace(x.ProductId)).ToList();
        if (itemRows.Count == 0)
        {
            return ServiceResult.Failure("Please add at least one GRN item.");
        }

        var productIds = itemRows.Select(x => x.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds);
        var purchaseItems = new List<PurchaseItem>();
        decimal total = 0;

        foreach (var item in itemRows)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId);
            if (product is null)
            {
                return ServiceResult.Failure("One of the selected products could not be found.");
            }

            purchaseItems.Add(new PurchaseItem
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Quantity = item.Quantity,
                CostPrice = item.CostPrice,
                LineTotal = item.Quantity * item.CostPrice
            });
            total += item.Quantity * item.CostPrice;
        }

        var purchase = new Purchase
        {
            GrnNumber = string.IsNullOrWhiteSpace(model.GrnNumber) ? await salesService.GetNextGrnNumberAsync() : model.GrnNumber,
            SupplierId = model.SupplierId,
            SupplierName = supplier.Name,
            GrnDate = model.GrnDate,
            TotalAmount = total,
            Notes = model.Notes,
            ReceivedByUserId = currentUser.UserId,
            ReceivedByName = currentUser.FullName,
            Items = purchaseItems,
            CreatedBy = currentUser.UserName
        };

        await purchaseRepository.AddAsync(purchase);

        foreach (var item in purchaseItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var beforeStock = product.CurrentStock;
            product.CurrentStock += item.Quantity;
            product.CostPrice = item.CostPrice;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = currentUser.UserName;
            await productRepository.UpdateAsync(product);

            await inventoryService.RecordMovementAsync(new InventoryMovement
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                MovementType = StockMovementType.Purchase,
                Quantity = item.Quantity,
                BeforeStock = beforeStock,
                AfterStock = product.CurrentStock,
                ReferenceType = "GRN",
                ReferenceNo = purchase.GrnNumber,
                Remarks = "Stock increased after goods receipt.",
                UserId = currentUser.UserId,
                UserName = currentUser.FullName,
                CreatedBy = currentUser.UserName
            });
        }

        return ServiceResult.Success("GRN saved successfully.");
    }

    public async Task<PurchaseDetailViewModel?> GetDetailsAsync(string id)
    {
        var purchase = await purchaseRepository.GetByIdAsync(id);
        if (purchase is null)
        {
            return null;
        }

        return new PurchaseDetailViewModel
        {
            GrnNumber = purchase.GrnNumber,
            SupplierName = purchase.SupplierName,
            GrnDate = purchase.GrnDate,
            TotalAmount = purchase.TotalAmount,
            Notes = purchase.Notes,
            Items = purchase.Items.Select(x => new PurchaseDetailItemViewModel
            {
                ProductName = x.ProductName,
                Quantity = x.Quantity,
                CostPrice = x.CostPrice,
                LineTotal = x.LineTotal
            }).ToList()
        };
    }
}
