using Microsoft.AspNetCore.Identity;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using MongoDB.Driver;

namespace ModernPosSystem.Services;

public class DataSeeder(
    IRepository<Role> roleRepository,
    IRepository<AppForm> formRepository,
    IRepository<RoleFormPermission> permissionRepository,
    IRepository<SystemUser> userRepository,
    IRepository<Category> categoryRepository,
    IRepository<Supplier> supplierRepository,
    IRepository<Customer> customerRepository,
    IRepository<Product> productRepository,
    IRepository<Purchase> purchaseRepository,
    IRepository<Sale> saleRepository,
    IRepository<AppSetting> settingRepository,
    IMongoDbContext mongoDbContext,
    IPasswordHasher<SystemUser> passwordHasher) : IDataSeeder
{
    public async Task SeedAsync()
    {
        await EnsureIndexesAsync();
        await SeedRolesFormsAndPermissionsAsync();
        await SeedUsersAsync();
        await SeedMasterDataAsync();
        await SeedTransactionsAsync();
        await SeedSettingsAsync();
    }

    private async Task EnsureIndexesAsync()
    {
        await mongoDbContext.GetCollection<SystemUser>().Indexes.CreateManyAsync(
        [
            new CreateIndexModel<SystemUser>(Builders<SystemUser>.IndexKeys.Ascending(x => x.Username), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<SystemUser>(Builders<SystemUser>.IndexKeys.Ascending(x => x.Email))
        ]);

        await mongoDbContext.GetCollection<Product>().Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(x => x.ProductCode), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(x => x.Barcode), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(x => x.ForecastItemCode))
        ]);

        await mongoDbContext.GetCollection<Customer>().Indexes.CreateOneAsync(
            new CreateIndexModel<Customer>(Builders<Customer>.IndexKeys.Ascending(x => x.CustomerCode), new CreateIndexOptions { Unique = true }));

        await mongoDbContext.GetCollection<Supplier>().Indexes.CreateOneAsync(
            new CreateIndexModel<Supplier>(Builders<Supplier>.IndexKeys.Ascending(x => x.SupplierCode), new CreateIndexOptions { Unique = true }));

        await mongoDbContext.GetCollection<Sale>().Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Sale>(Builders<Sale>.IndexKeys.Ascending(x => x.InvoiceNo), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Sale>(Builders<Sale>.IndexKeys.Ascending(x => x.SaleDate))
        ]);

        await mongoDbContext.GetCollection<Purchase>().Indexes.CreateOneAsync(
            new CreateIndexModel<Purchase>(Builders<Purchase>.IndexKeys.Ascending(x => x.GrnDate)));

        await mongoDbContext.GetCollection<ForecastDocument>().Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ForecastDocument>(Builders<ForecastDocument>.IndexKeys
                .Ascending(x => x.StoreId)
                .Ascending(x => x.ProductId)
                .Ascending(x => x.ForecastStartDate)),
            new CreateIndexModel<ForecastDocument>(Builders<ForecastDocument>.IndexKeys.Ascending(x => x.ForecastGeneratedAt))
        ]);
    }

    private async Task SeedRolesFormsAndPermissionsAsync()
    {
        var roles = await roleRepository.GetAllAsync();
        if (roles.Count == 0)
        {
            await roleRepository.AddRangeAsync(
            [
                new Role { Name = "Admin", Description = "Full system administration", CreatedBy = "seed" },
                new Role { Name = "Cashier", Description = "POS and customer operations", CreatedBy = "seed" },
                new Role { Name = "Manager", Description = "Store oversight, reports and forecasting", CreatedBy = "seed" }
            ]);
            roles = await roleRepository.GetAllAsync();
        }

        var desiredForms = new List<AppForm>
        {
            new() { Key = FormModules.PosBilling, Name = "POS Billing", Icon = "bi bi-cart3", Route = "/Pos", SortOrder = 1, CreatedBy = "seed" },
            new() { Key = FormModules.Dashboard, Name = "Dashboard", Icon = "bi bi-grid", Route = "/Dashboard", SortOrder = 2, CreatedBy = "seed" },
            new() { Key = FormModules.Products, Name = "Products", Icon = "bi bi-box", Route = "/Products", SortOrder = 3, CreatedBy = "seed" },
            new() { Key = FormModules.Categories, Name = "Categories", Icon = "bi bi-tags", Route = "/Categories", SortOrder = 4, CreatedBy = "seed" },
            new() { Key = FormModules.Inventory, Name = "Inventory", Icon = "bi bi-stack", Route = "/Inventory", SortOrder = 5, CreatedBy = "seed" },
            new() { Key = FormModules.Customers, Name = "Customers", Icon = "bi bi-people", Route = "/Customers", SortOrder = 6, CreatedBy = "seed" },
            new() { Key = FormModules.Suppliers, Name = "Suppliers", Icon = "bi bi-truck", Route = "/Suppliers", SortOrder = 7, CreatedBy = "seed" },
            new() { Key = FormModules.Purchases, Name = "Purchases / GRN", Icon = "bi bi-journal-plus", Route = "/Purchases", SortOrder = 8, CreatedBy = "seed" },
            new() { Key = FormModules.SalesHistory, Name = "Sales History", Icon = "bi bi-receipt-cutoff", Route = "/SalesHistory", SortOrder = 9, CreatedBy = "seed" },
            new() { Key = FormModules.Reports, Name = "Reports", Icon = "bi bi-file-earmark-bar-graph", Route = "/Reports", SortOrder = 10, CreatedBy = "seed" },
            new() { Key = FormModules.Users, Name = "Users", Icon = "bi bi-person-gear", Route = "/Users", SortOrder = 11, CreatedBy = "seed" },
            new() { Key = FormModules.RolePermissions, Name = "Role Permissions", Icon = "bi bi-shield-lock", Route = "/RolePermissions", SortOrder = 12, CreatedBy = "seed" },
            new() { Key = FormModules.Forecasting, Name = "Forecasting", Icon = "bi bi-cpu", Route = "/Forecasting", SortOrder = 13, CreatedBy = "seed" },
            new() { Key = FormModules.Settings, Name = "Settings", Icon = "bi bi-sliders", Route = "/Settings", SortOrder = 14, CreatedBy = "seed" }
        };

        var existingForms = await formRepository.GetAllAsync();
        foreach (var desiredForm in desiredForms)
        {
            var existingForm = existingForms.FirstOrDefault(x => x.Key.Equals(desiredForm.Key, StringComparison.OrdinalIgnoreCase));
            if (existingForm is null)
            {
                await formRepository.AddAsync(desiredForm);
                continue;
            }

            existingForm.Name = desiredForm.Name;
            existingForm.Icon = desiredForm.Icon;
            existingForm.Route = desiredForm.Route;
            existingForm.SortOrder = desiredForm.SortOrder;
            existingForm.IsActive = true;
            existingForm.UpdatedAt = DateTime.UtcNow;
            existingForm.UpdatedBy = "seed";
            await formRepository.UpdateAsync(existingForm);
        }

        var adminRole = roles.First(x => x.Name == "Admin");
        var cashierRole = roles.First(x => x.Name == "Cashier");
        var managerRole = roles.First(x => x.Name == "Manager");
        var forms = (await formRepository.GetAllAsync(x => x.IsActive)).OrderBy(x => x.SortOrder).ToList();
        var permissions = await permissionRepository.GetAllAsync();

        var cashierKeys = new[] { FormModules.PosBilling, FormModules.Customers, FormModules.SalesHistory };
        var managerKeys = new[] { FormModules.Dashboard, FormModules.Inventory, FormModules.Reports, FormModules.Forecasting };

        var roleRules = new Dictionary<string, Func<string, bool>>
        {
            [adminRole.Id] = _ => true,
            [cashierRole.Id] = key => cashierKeys.Contains(key),
            [managerRole.Id] = key => managerKeys.Contains(key)
        };

        foreach (var roleRule in roleRules)
        {
            foreach (var form in forms)
            {
                var permission = permissions.FirstOrDefault(x => x.RoleId == roleRule.Key && x.FormKey == form.Key);
                if (permission is null)
                {
                    await permissionRepository.AddAsync(new RoleFormPermission
                    {
                        RoleId = roleRule.Key,
                        FormKey = form.Key,
                        CanAccess = roleRule.Value(form.Key),
                        CreatedBy = "seed"
                    });
                    continue;
                }

                permission.CanAccess = roleRule.Value(form.Key);
                permission.IsActive = true;
                permission.UpdatedAt = DateTime.UtcNow;
                permission.UpdatedBy = "seed";
                await permissionRepository.UpdateAsync(permission);
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        var roles = await roleRepository.GetAllAsync();
        var adminRole = roles.First(x => x.Name == "Admin");
        var cashierRole = roles.First(x => x.Name == "Cashier");
        var managerRole = roles.First(x => x.Name == "Manager");

        await UpsertSeedUserAsync("System Administrator", "admin", "admin@modernpos.local", adminRole, "Admin@123");
        await UpsertSeedUserAsync("Cashier User", "cashier", "cashier@modernpos.local", cashierRole, "Cashier@123");
        await UpsertSeedUserAsync("Store Manager", "manager", "manager@modernpos.local", managerRole, "Manager@123");
    }

    private async Task SeedMasterDataAsync()
    {
        if (await categoryRepository.CountAsync() == 0)
        {
            await categoryRepository.AddRangeAsync(
            [
                new Category { CategoryId = "CAT-001", CategoryName = "Beverages", Description = "Drinks and refreshment items", CreatedBy = "seed" },
                new Category { CategoryId = "CAT-002", CategoryName = "Snacks", Description = "Quick snack products", CreatedBy = "seed" },
                new Category { CategoryId = "CAT-003", CategoryName = "Personal Care", Description = "Daily personal care essentials", CreatedBy = "seed" },
                new Category { CategoryId = "CAT-004", CategoryName = "Household", Description = "Home and cleaning items", CreatedBy = "seed" },
                new Category { CategoryId = "CAT-005", CategoryName = "Grocery", Description = "Staple grocery items", CreatedBy = "seed" }
            ]);
        }

        if (await supplierRepository.CountAsync() == 0)
        {
            await supplierRepository.AddRangeAsync(
            [
                new Supplier { SupplierCode = "SUP-001", SupplierName = "FreshLine Distributors", ContactPerson = "Nimal Silva", Phone = "0771000001", Email = "sales@freshline.local", Address = "Colombo 05", CreatedBy = "seed" },
                new Supplier { SupplierCode = "SUP-002", SupplierName = "Retail Hub Traders", ContactPerson = "Kavindi Perera", Phone = "0771000002", Email = "contact@retailhub.local", Address = "Kandy", CreatedBy = "seed" },
                new Supplier { SupplierCode = "SUP-003", SupplierName = "Prime Wholesale", ContactPerson = "Ayesha Fernando", Phone = "0771000003", Email = "support@primewholesale.local", Address = "Galle", CreatedBy = "seed" },
                new Supplier { SupplierCode = "SUP-004", SupplierName = "Urban Supply Co.", ContactPerson = "Ruwan Jayasuriya", Phone = "0771000004", Email = "hello@urbansupply.local", Address = "Negombo", CreatedBy = "seed" },
                new Supplier { SupplierCode = "SUP-005", SupplierName = "ValueMax Imports", ContactPerson = "Tharushi De Mel", Phone = "0771000005", Email = "orders@valuemax.local", Address = "Kurunegala", CreatedBy = "seed" }
            ]);
        }

        if (await customerRepository.CountAsync() == 0)
        {
            await customerRepository.AddRangeAsync(
            [
                new Customer { CustomerCode = "WALKIN", FullName = "Walk-In Customer", Phone = "-", Email = "", Address = "", LoyaltyPoints = 0, CreatedBy = "seed" },
                new Customer { CustomerCode = "CUS-001", FullName = "Ishara Wijesinghe", Phone = "0712345678", Email = "ishara@example.com", Address = "Maharagama", LoyaltyPoints = 120, CreatedBy = "seed" },
                new Customer { CustomerCode = "CUS-002", FullName = "Dinuka Abeysekara", Phone = "0723456789", Email = "dinuka@example.com", Address = "Panadura", LoyaltyPoints = 86, CreatedBy = "seed" },
                new Customer { CustomerCode = "CUS-003", FullName = "Nethmi Gunasekara", Phone = "0734567890", Email = "nethmi@example.com", Address = "Matara", LoyaltyPoints = 44, CreatedBy = "seed" },
                new Customer { CustomerCode = "CUS-004", FullName = "Prabath Senanayake", Phone = "0745678901", Email = "prabath@example.com", Address = "Kurunegala", LoyaltyPoints = 63, CreatedBy = "seed" }
            ]);
        }

        if (await productRepository.CountAsync() > 0)
        {
            var existingProducts = await productRepository.GetAllAsync(x => x.IsActive);
            var usedCodes = existingProducts.Where(x => x.ForecastItemCode > 0).Select(x => x.ForecastItemCode).ToHashSet();
            var nextForecastCode = 1;
            foreach (var product in existingProducts.OrderBy(x => x.ProductName))
            {
                if (product.ForecastItemCode <= 0)
                {
                    while (usedCodes.Contains(nextForecastCode))
                    {
                        nextForecastCode++;
                    }
                    product.ForecastItemCode = nextForecastCode;
                    usedCodes.Add(nextForecastCode);
                }

                product.SafetyStock ??= Math.Max(product.ReorderLevel, 5m);
                product.SupplierLeadTimeDays ??= 3;
                nextForecastCode = Math.Max(nextForecastCode, product.ForecastItemCode + 1);
                await productRepository.UpdateAsync(product);
            }
            return;
        }

        var categories = await categoryRepository.GetAllAsync();
        var suppliers = await supplierRepository.GetAllAsync();
        var productSeeds = new[]
        {
            ("Mineral Water 500ml", "Beverages", 80m, 120m, 45m, 12m),
            ("Orange Juice 1L", "Beverages", 280m, 360m, 20m, 8m),
            ("Iced Coffee Can", "Beverages", 150m, 220m, 16m, 6m),
            ("Energy Drink", "Beverages", 210m, 290m, 14m, 5m),
            ("Chocolate Cookies", "Snacks", 180m, 260m, 26m, 10m),
            ("Potato Chips", "Snacks", 130m, 210m, 10m, 8m),
            ("Mixed Nuts Pack", "Snacks", 220m, 340m, 11m, 6m),
            ("Granola Bar", "Snacks", 95m, 150m, 32m, 9m),
            ("Toothpaste Fresh Mint", "Personal Care", 190m, 280m, 18m, 8m),
            ("Shampoo Herbal", "Personal Care", 340m, 480m, 13m, 7m),
            ("Hand Wash", "Personal Care", 240m, 360m, 12m, 5m),
            ("Body Lotion", "Personal Care", 420m, 560m, 9m, 4m),
            ("Dish Wash Liquid", "Household", 210m, 320m, 17m, 6m),
            ("Laundry Powder 1kg", "Household", 460m, 620m, 15m, 5m),
            ("Floor Cleaner", "Household", 260m, 390m, 8m, 4m),
            ("Tissue Pack", "Household", 90m, 150m, 40m, 12m),
            ("Basmati Rice 5kg", "Grocery", 1450m, 1680m, 10m, 5m),
            ("Sugar 1kg", "Grocery", 220m, 270m, 22m, 10m),
            ("Tea Pack 400g", "Grocery", 590m, 760m, 12m, 6m),
            ("Cooking Oil 1L", "Grocery", 490m, 620m, 19m, 8m)
        };

        var products = productSeeds.Select((seed, index) =>
        {
            var category = categories.First(x => x.CategoryName == seed.Item2);
            var supplier = suppliers[index % suppliers.Count];
            return new Product
            {
                ProductId = $"PRD-{index + 1:000}",
                ProductCode = $"P{index + 1:0000}",
                Barcode = $"89012345{index + 1:000}",
                ProductName = seed.Item1,
                Description = $"{seed.Item1} premium retail item",
                CategoryId = category.Id,
                CategoryName = category.CategoryName,
                SupplierId = supplier.Id,
                SupplierName = supplier.SupplierName,
                CostPrice = seed.Item3,
                SellingPrice = seed.Item4,
                CurrentStock = seed.Item5,
                ReorderLevel = seed.Item6,
                ForecastItemCode = index + 1,
                SafetyStock = Math.Max(seed.Item6, 5m),
                SupplierLeadTimeDays = 2 + (index % 4),
                Unit = "pcs",
                ImageUrl = "/images/product-placeholder.svg",
                CreatedBy = "seed"
            };
        }).ToList();

        await productRepository.AddRangeAsync(products);
    }

    private async Task SeedTransactionsAsync()
    {
        var products = await productRepository.GetAllAsync();
        var suppliers = await supplierRepository.GetAllAsync();
        var customers = await customerRepository.GetAllAsync();
        var users = await userRepository.GetAllAsync();
        var cashier = users.FirstOrDefault(x => x.Username == "cashier") ?? users.First();
        var purchaseCount = await purchaseRepository.CountAsync();
        var saleCount = await saleRepository.CountAsync();

        if (purchaseCount == 0)
        {
            await purchaseRepository.AddAsync(new Purchase
            {
                GrnNumber = "GRN-SEED-001",
                SupplierId = suppliers.First().Id,
                SupplierName = suppliers.First().SupplierName,
                GrnDate = DateTime.UtcNow.AddDays(-3),
                TotalAmount = 8600,
                ReceivedByName = "System Seeder",
                CreatedBy = "seed",
                Items =
                [
                    new PurchaseItem { ProductId = products[0].Id, ProductCode = products[0].ProductCode, ProductName = products[0].ProductName, Quantity = 20, CostPrice = products[0].CostPrice, LineTotal = 20 * products[0].CostPrice },
                    new PurchaseItem { ProductId = products[4].Id, ProductCode = products[4].ProductCode, ProductName = products[4].ProductName, Quantity = 15, CostPrice = products[4].CostPrice, LineTotal = 15 * products[4].CostPrice }
                ]
            });
        }

        if (saleCount >= 60)
        {
            return;
        }

        var seededSales = new List<Sale>();
        var seededRandom = new Random(42);
        var walkInCustomer = customers.First(x => x.CustomerCode == "WALKIN");
        var historyStart = DateTime.UtcNow.Date.AddDays(-(90 - (int)saleCount));

        for (var dayOffset = 0; dayOffset < (90 - saleCount); dayOffset++)
        {
            var saleDate = historyStart.AddDays(dayOffset).AddHours(9 + (dayOffset % 8));
            var pickedProducts = products
                .OrderBy(product => (product.ForecastItemCode + dayOffset) % 9)
                .Take(6)
                .ToList();

            var items = new List<SaleItem>();
            decimal subtotal = 0;
            foreach (var product in pickedProducts)
            {
                var seasonalBase = 1 + (product.ForecastItemCode % 4);
                var quantity = seasonalBase + (dayOffset % 5);
                if (product.CategoryName == "Beverages" || product.CategoryName == "Snacks")
                {
                    quantity += dayOffset % 3;
                }

                var lineTotal = quantity * product.SellingPrice;
                subtotal += lineTotal;
                items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Barcode = product.Barcode,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    DiscountAmount = 0,
                    LineTotal = lineTotal
                });
            }

            var discount = dayOffset % 6 == 0 ? Math.Round(subtotal * 0.03m, 2) : 0;
            var grandTotal = subtotal - discount;
            seededSales.Add(new Sale
            {
                InvoiceNo = $"INV-SEED-{saleCount + dayOffset + 1:0000}",
                SaleDate = saleDate,
                CustomerId = walkInCustomer.Id,
                CustomerName = walkInCustomer.FullName,
                IsWalkInCustomer = true,
                CashierUserId = cashier.Id,
                CashierName = cashier.FullName,
                PaymentMethod = dayOffset % 4 == 0 ? PaymentMethod.Card : PaymentMethod.Cash,
                Subtotal = subtotal,
                DiscountTotal = discount,
                TaxAmount = 0,
                GrandTotal = grandTotal,
                AmountReceived = grandTotal + seededRandom.Next(0, 100),
                BalanceReturn = seededRandom.Next(0, 50),
                CreatedBy = "seed",
                Items = items
            });
        }

        await saleRepository.AddRangeAsync(seededSales);
    }

    private async Task SeedSettingsAsync()
    {
        var existingSettings = await settingRepository.GetAllAsync();
        var defaults = new List<AppSetting>
        {
            new() { Key = "ShopName", Value = "Modern POS Research Lab", Description = "Brand name shown in the header and invoice", CreatedBy = "seed" },
            new() { Key = "DefaultTaxRate", Value = "0", Description = "Default tax percentage placeholder for later use", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.ForecastingProvider, Value = "PythonFastApiXGBoost", Description = "Configured forecasting provider integration point", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.LoyaltyEarnRate, Value = "100", Description = "Award 1 loyalty point for every configured currency amount spent", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.LoyaltyRedeemValue, Value = "1", Description = "Currency value of a single loyalty point during checkout", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.LoyaltyMinimumRedeemPoints, Value = "1", Description = "Minimum loyalty points that can be redeemed in one transaction", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.ForecastLastAutoGeneratedAtUtc, Value = string.Empty, Description = "Last successful automatic forecast run timestamp (UTC)", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.ForecastLastAutoGeneratedDateLocal, Value = string.Empty, Description = "Last successful automatic forecast local date marker", CreatedBy = "seed" },
            new() { Key = AppSettingKeys.ForecastLastManualGeneratedAtUtc, Value = string.Empty, Description = "Last successful manual forecast run timestamp (UTC)", CreatedBy = "seed" }
        };

        var missingSettings = defaults
            .Where(setting => existingSettings.All(existing => existing.Key != setting.Key))
            .ToList();

        if (missingSettings.Count > 0)
        {
            await settingRepository.AddRangeAsync(missingSettings);
        }
    }

    private SystemUser CreateUser(string fullName, string username, string email, Role role, string password)
    {
        var user = new SystemUser
        {
            FullName = fullName,
            Username = username,
            Email = email,
            RoleId = role.Id,
            RoleName = role.Name,
            CreatedBy = "seed"
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private async Task UpsertSeedUserAsync(string fullName, string username, string email, Role role, string password)
    {
        var user = await userRepository.GetFirstOrDefaultAsync(x => x.Username == username);
        if (user is null)
        {
            await userRepository.AddAsync(CreateUser(fullName, username, email, role, password));
            return;
        }

        user.FullName = fullName;
        user.Email = email;
        user.RoleId = role.Id;
        user.RoleName = role.Name;
        user.IsActive = true;
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = "seed";
        await userRepository.UpdateAsync(user);
    }
}
