using Microsoft.AspNetCore.Identity;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Services;

public class ProductService(
    IRepository<Product> productRepository,
    IRepository<Category> categoryRepository,
    IRepository<Supplier> supplierRepository,
    IImageStorageService imageStorageService) : IProductService
{
    public async Task<List<ProductListItemViewModel>> GetListAsync(string? searchTerm = null, string? categoryId = null)
    {
        var products = await productRepository.GetAllAsync(x => x.IsActive &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
             x.ProductName.ToLower().Contains(searchTerm.ToLower()) ||
             x.ProductCode.ToLower().Contains(searchTerm.ToLower()) ||
             x.Barcode.ToLower().Contains(searchTerm.ToLower())) &&
            (string.IsNullOrWhiteSpace(categoryId) || x.CategoryId == categoryId));

        return products
            .OrderBy(x => x.ProductName)
            .Select(MapProductListItem)
            .ToList();
    }

    public async Task<ProductFormViewModel> GetFormAsync(string? id = null)
    {
        var products = await productRepository.GetAllAsync(x => x.IsActive);
        var model = new ProductFormViewModel
        {
            ProductId = $"PRD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ForecastItemCode = products.Count == 0 ? 1 : products.Max(x => x.ForecastItemCode) + 1,
            SafetyStock = 5,
            SupplierLeadTimeDays = 3
        };

        if (string.IsNullOrWhiteSpace(id))
        {
            return await PrepareFormAsync(model);
        }

        var product = await productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return await PrepareFormAsync(model);
        }

        model.Id = product.Id;
        model.ProductId = product.ProductId;
        model.ProductCode = product.ProductCode;
        model.Barcode = product.Barcode;
        model.ProductName = product.ProductName;
        model.Description = product.Description;
        model.CategoryId = product.CategoryId;
        model.SupplierId = product.SupplierId;
        model.CostPrice = product.CostPrice;
        model.SellingPrice = product.SellingPrice;
        model.CurrentStock = product.CurrentStock;
        model.ReorderLevel = product.ReorderLevel;
        model.ForecastItemCode = product.ForecastItemCode;
        model.SafetyStock = product.SafetyStock;
        model.SupplierLeadTimeDays = product.SupplierLeadTimeDays;
        model.Unit = product.Unit;
        model.ImageUrl = product.ImageUrl;
        model.IsActive = product.IsActive;
        return await PrepareFormAsync(model);
    }

    public async Task<ProductFormViewModel> PrepareFormAsync(ProductFormViewModel model)
    {
        model.Categories = (await categoryRepository.GetAllAsync(x => x.IsActive))
            .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.CategoryName })
            .ToList();
        model.Suppliers = (await supplierRepository.GetAllAsync(x => x.IsActive))
            .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.SupplierName })
            .ToList();
        return model;
    }

    public async Task<ServiceResult> SaveAsync(ProductFormViewModel model, string userName)
    {
        var category = await categoryRepository.GetByIdAsync(model.CategoryId);
        var supplier = await supplierRepository.GetByIdAsync(model.SupplierId);
        if (category is null || supplier is null)
        {
            return ServiceResult.Failure("Please select valid category and supplier.");
        }

        var duplicate = await productRepository.GetFirstOrDefaultAsync(x =>
            (x.ProductCode.ToLower() == model.ProductCode.ToLower() || x.Barcode.ToLower() == model.Barcode.ToLower()) &&
            x.Id != (model.Id ?? string.Empty));

        if (duplicate is not null)
        {
            return ServiceResult.Failure("Product code or barcode already exists.");
        }

        var imageUrl = model.ImageUrl?.Trim() ?? string.Empty;
        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            var uploadResult = await imageStorageService.UploadProductImageAsync(model.ImageFile);
            if (!uploadResult.Succeeded || string.IsNullOrWhiteSpace(uploadResult.Data))
            {
                return ServiceResult.Failure(uploadResult.Message);
            }

            imageUrl = uploadResult.Data;
        }

        if (string.IsNullOrWhiteSpace(model.Id))
        {
            await productRepository.AddAsync(new Product
            {
                ProductId = model.ProductId,
                ProductCode = model.ProductCode.Trim(),
                Barcode = model.Barcode.Trim(),
                ProductName = model.ProductName.Trim(),
                Description = model.Description.Trim(),
                CategoryId = category.Id,
                CategoryName = category.CategoryName,
                SupplierId = supplier.Id,
                SupplierName = supplier.SupplierName,
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                CurrentStock = model.CurrentStock,
                ReorderLevel = model.ReorderLevel,
                ForecastItemCode = model.ForecastItemCode,
                SafetyStock = model.SafetyStock,
                SupplierLeadTimeDays = model.SupplierLeadTimeDays,
                Unit = model.Unit,
                ImageUrl = imageUrl,
                IsActive = model.IsActive,
                CreatedBy = userName
            });

            return ServiceResult.Success("Product created successfully.");
        }

        var existing = await productRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("Product not found.");
        }

        existing.ProductId = model.ProductId;
        existing.ProductCode = model.ProductCode.Trim();
        existing.Barcode = model.Barcode.Trim();
        existing.ProductName = model.ProductName.Trim();
        existing.Description = model.Description.Trim();
        existing.CategoryId = category.Id;
        existing.CategoryName = category.CategoryName;
        existing.SupplierId = supplier.Id;
        existing.SupplierName = supplier.SupplierName;
        existing.CostPrice = model.CostPrice;
        existing.SellingPrice = model.SellingPrice;
        existing.CurrentStock = model.CurrentStock;
        existing.ReorderLevel = model.ReorderLevel;
        existing.ForecastItemCode = model.ForecastItemCode;
        existing.SafetyStock = model.SafetyStock;
        existing.SupplierLeadTimeDays = model.SupplierLeadTimeDays;
        existing.Unit = model.Unit;
        existing.ImageUrl = imageUrl;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;

        await productRepository.UpdateAsync(existing);
        return ServiceResult.Success("Product updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(string id, string userName)
    {
        await productRepository.SoftDeleteAsync(id, userName);
        return ServiceResult.Success("Product removed successfully.");
    }

    public async Task<List<LookupOptionViewModel>> GetLookupAsync() =>
        (await productRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.ProductName)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = $"{x.ProductName} ({x.ProductCode})" })
        .ToList();

    public async Task<List<PosProductCardViewModel>> SearchForPosAsync(string? query = null)
    {
        var products = await productRepository.GetAllAsync(x => x.IsActive &&
            (string.IsNullOrWhiteSpace(query) ||
             x.ProductName.ToLower().Contains(query.ToLower()) ||
             x.ProductCode.ToLower().Contains(query.ToLower()) ||
             x.Barcode.ToLower().Contains(query.ToLower())));

        return products
            .OrderBy(x => x.ProductName)
            .Select(x => new PosProductCardViewModel
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                Barcode = x.Barcode,
                ProductName = x.ProductName,
                CategoryName = x.CategoryName,
                SellingPrice = x.SellingPrice,
                CurrentStock = x.CurrentStock,
                ReorderLevel = x.ReorderLevel,
                ImageUrl = string.IsNullOrWhiteSpace(x.ImageUrl) ? "/images/product-placeholder.svg" : x.ImageUrl
            })
            .ToList();
    }

    public Task<Product?> GetByIdAsync(string id) => productRepository.GetByIdAsync(id);

    private static ProductListItemViewModel MapProductListItem(Product product) => new()
    {
        Id = product.Id,
        ProductCode = product.ProductCode,
        Barcode = product.Barcode,
        ProductName = product.ProductName,
        CategoryName = product.CategoryName,
        SupplierName = product.SupplierName,
        SellingPrice = product.SellingPrice,
        CurrentStock = product.CurrentStock,
        ReorderLevel = product.ReorderLevel,
        ForecastItemCode = product.ForecastItemCode,
        IsActive = product.IsActive,
        StockStatus = product.CurrentStock <= 0 ? "Out of Stock" : product.CurrentStock <= product.ReorderLevel ? "Low Stock" : "In Stock",
        ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? "/images/product-placeholder.svg" : product.ImageUrl
    };
}

public class CategoryService(
    IRepository<Category> categoryRepository,
    IRepository<Product> productRepository) : ICategoryService
{
    public async Task<List<CategoryListItemViewModel>> GetListAsync()
    {
        var categories = await categoryRepository.GetAllAsync();
        var products = await productRepository.GetAllAsync(x => x.IsActive);

        return categories
            .OrderBy(x => x.CategoryName)
            .Select(x => new CategoryListItemViewModel
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                CategoryName = x.CategoryName,
                Description = x.Description,
                IsActive = x.IsActive,
                ProductCount = products.Count(p => p.CategoryId == x.Id)
            })
            .ToList();
    }

    public async Task<CategoryFormViewModel> GetFormAsync(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new CategoryFormViewModel { CategoryId = $"CAT-{DateTime.UtcNow:yyyyMMddHHmmss}" };
        }

        var category = await categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return new CategoryFormViewModel();
        }

        return new CategoryFormViewModel
        {
            Id = category.Id,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            IsActive = category.IsActive
        };
    }

    public async Task<ServiceResult> SaveAsync(CategoryFormViewModel model, string userName)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            await categoryRepository.AddAsync(new Category
            {
                CategoryId = model.CategoryId,
                CategoryName = model.CategoryName.Trim(),
                Description = model.Description.Trim(),
                IsActive = model.IsActive,
                CreatedBy = userName
            });

            return ServiceResult.Success("Category created successfully.");
        }

        var existing = await categoryRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("Category not found.");
        }

        existing.CategoryName = model.CategoryName.Trim();
        existing.Description = model.Description.Trim();
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await categoryRepository.UpdateAsync(existing);
        return ServiceResult.Success("Category updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(string id, string userName)
    {
        await categoryRepository.SoftDeleteAsync(id, userName);
        return ServiceResult.Success("Category removed successfully.");
    }

    public async Task<List<LookupOptionViewModel>> GetLookupAsync() =>
        (await categoryRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.CategoryName)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.CategoryName })
        .ToList();
}

public class CustomerService(
    IRepository<Customer> customerRepository,
    IRepository<Sale> saleRepository) : ICustomerService
{
    public async Task<List<CustomerListItemViewModel>> GetListAsync(string? searchTerm = null) =>
        (await customerRepository.GetAllAsync(x => x.IsActive &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
             x.FullName.ToLower().Contains(searchTerm.ToLower()) ||
             x.CustomerCode.ToLower().Contains(searchTerm.ToLower()) ||
             x.Phone.ToLower().Contains(searchTerm.ToLower()))))
        .OrderBy(x => x.FullName)
        .Select(x => new CustomerListItemViewModel
        {
            Id = x.Id,
            CustomerCode = x.CustomerCode,
            FullName = x.FullName,
            Phone = x.Phone,
            Email = x.Email,
            LoyaltyPoints = x.LoyaltyPoints,
            IsActive = x.IsActive
        })
        .ToList();

    public async Task<CustomerFormViewModel> GetFormAsync(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new CustomerFormViewModel { CustomerCode = $"CUS-{DateTime.UtcNow:yyyyMMddHHmmss}" };
        }

        var customer = await customerRepository.GetByIdAsync(id);
        if (customer is null)
        {
            return new CustomerFormViewModel();
        }

        return new CustomerFormViewModel
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive
        };
    }

    public async Task<ServiceResult> SaveAsync(CustomerFormViewModel model, string userName)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            await customerRepository.AddAsync(new Customer
            {
                CustomerCode = model.CustomerCode,
                FullName = model.FullName.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                LoyaltyPoints = model.LoyaltyPoints,
                IsActive = model.IsActive,
                CreatedBy = userName
            });

            return ServiceResult.Success("Customer created successfully.");
        }

        var existing = await customerRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("Customer not found.");
        }

        existing.CustomerCode = model.CustomerCode;
        existing.FullName = model.FullName.Trim();
        existing.Phone = model.Phone.Trim();
        existing.Email = model.Email.Trim();
        existing.Address = model.Address.Trim();
        existing.LoyaltyPoints = model.LoyaltyPoints;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await customerRepository.UpdateAsync(existing);
        return ServiceResult.Success("Customer updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(string id, string userName)
    {
        await customerRepository.SoftDeleteAsync(id, userName);
        return ServiceResult.Success("Customer removed successfully.");
    }

    public async Task<List<LookupOptionViewModel>> GetLookupAsync() =>
        (await customerRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.FullName)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = $"{x.FullName} ({x.CustomerCode})" })
        .ToList();

    public async Task<List<PosCustomerOptionViewModel>> GetPosLookupAsync() =>
        (await customerRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.FullName)
        .Select(x => new PosCustomerOptionViewModel
        {
            Id = x.Id,
            Name = x.FullName,
            CustomerCode = x.CustomerCode,
            LoyaltyPoints = x.LoyaltyPoints,
            IsWalkIn = x.CustomerCode == "WALKIN"
        })
        .ToList();

    public Task<Customer?> GetByIdAsync(string id) => customerRepository.GetByIdAsync(id);

    public async Task<string> GetWalkInCustomerIdAsync() =>
        (await customerRepository.GetFirstOrDefaultAsync(x => x.CustomerCode == "WALKIN" && x.IsActive))?.Id ?? string.Empty;

    public async Task<List<SalesHistoryItemViewModel>> GetPurchaseHistoryAsync(string customerId) =>
        (await saleRepository.GetAllAsync(x => x.IsActive && x.CustomerId == customerId))
        .OrderByDescending(x => x.SaleDate)
        .Select(x => new SalesHistoryItemViewModel
        {
            Id = x.Id,
            InvoiceNo = x.InvoiceNo,
            SaleDate = x.SaleDate,
            CustomerName = x.CustomerName,
            CashierName = x.CashierName,
            PaymentMethod = x.PaymentMethod switch
            {
                PaymentMethod.FundTransfer => "Fund Transfer",
                PaymentMethod.Split => "Split Payment",
                _ => x.PaymentMethod.ToString()
            },
            GrandTotal = x.NetPayable > 0 ? x.NetPayable : x.GrandTotal
        })
        .ToList();
}

public class SupplierService(IRepository<Supplier> supplierRepository) : ISupplierService
{
    public async Task<List<SupplierListItemViewModel>> GetListAsync(string? searchTerm = null) =>
        (await supplierRepository.GetAllAsync(x => x.IsActive &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
             x.SupplierName.ToLower().Contains(searchTerm.ToLower()) ||
             x.SupplierCode.ToLower().Contains(searchTerm.ToLower()) ||
             x.Phone.ToLower().Contains(searchTerm.ToLower()))))
        .OrderBy(x => x.SupplierName)
        .Select(x => new SupplierListItemViewModel
        {
            Id = x.Id,
            SupplierCode = x.SupplierCode,
            SupplierName = x.SupplierName,
            ContactPerson = x.ContactPerson,
            Phone = x.Phone,
            Email = x.Email,
            IsActive = x.IsActive
        })
        .ToList();

    public async Task<SupplierFormViewModel> GetFormAsync(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new SupplierFormViewModel { SupplierCode = $"SUP-{DateTime.UtcNow:yyyyMMddHHmmss}" };
        }

        var supplier = await supplierRepository.GetByIdAsync(id);
        if (supplier is null)
        {
            return new SupplierFormViewModel();
        }

        return new SupplierFormViewModel
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }

    public async Task<ServiceResult> SaveAsync(SupplierFormViewModel model, string userName)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            await supplierRepository.AddAsync(new Supplier
            {
                SupplierCode = model.SupplierCode,
                SupplierName = model.SupplierName.Trim(),
                ContactPerson = model.ContactPerson.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                IsActive = model.IsActive,
                CreatedBy = userName
            });

            return ServiceResult.Success("Supplier created successfully.");
        }

        var existing = await supplierRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("Supplier not found.");
        }

        existing.SupplierCode = model.SupplierCode;
        existing.SupplierName = model.SupplierName.Trim();
        existing.ContactPerson = model.ContactPerson.Trim();
        existing.Phone = model.Phone.Trim();
        existing.Email = model.Email.Trim();
        existing.Address = model.Address.Trim();
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await supplierRepository.UpdateAsync(existing);
        return ServiceResult.Success("Supplier updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(string id, string userName)
    {
        await supplierRepository.SoftDeleteAsync(id, userName);
        return ServiceResult.Success("Supplier removed successfully.");
    }

    public async Task<List<LookupOptionViewModel>> GetLookupAsync() =>
        (await supplierRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.SupplierName)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = $"{x.SupplierName} ({x.SupplierCode})" })
        .ToList();
}

public class UserService(
    IRepository<SystemUser> userRepository,
    IRepository<Role> roleRepository,
    IPasswordHasher<SystemUser> passwordHasher) : IUserService
{
    public async Task<List<UserListItemViewModel>> GetListAsync() =>
        (await userRepository.GetAllAsync())
        .OrderBy(x => x.FullName)
        .Select(x => new UserListItemViewModel
        {
            Id = x.Id,
            FullName = x.FullName,
            Username = x.Username,
            Email = x.Email,
            RoleName = x.RoleName,
            IsActive = x.IsActive
        })
        .ToList();

    public async Task<UserFormViewModel> GetFormAsync(string? id = null)
    {
        var roles = (await roleRepository.GetAllAsync(x => x.IsActive))
            .OrderBy(x => x.Name)
            .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.Name })
            .ToList();

        if (string.IsNullOrWhiteSpace(id))
        {
            return new UserFormViewModel { Roles = roles };
        }

        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return new UserFormViewModel { Roles = roles };
        }

        return new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            Roles = roles
        };
    }

    public async Task<ServiceResult> SaveAsync(UserFormViewModel model, string userName)
    {
        var role = await roleRepository.GetByIdAsync(model.RoleId);
        if (role is null)
        {
            return ServiceResult.Failure("Please select a valid role.");
        }

        var duplicate = await userRepository.GetFirstOrDefaultAsync(x =>
            (x.Username.ToLower() == model.Username.ToLower() || x.Email.ToLower() == model.Email.ToLower()) &&
            x.Id != (model.Id ?? string.Empty));

        if (duplicate is not null)
        {
            return ServiceResult.Failure("Username or email already exists.");
        }

        if (string.IsNullOrWhiteSpace(model.Id))
        {
            var user = new SystemUser
            {
                FullName = model.FullName.Trim(),
                Username = model.Username.Trim(),
                Email = model.Email.Trim(),
                RoleId = role.Id,
                RoleName = role.Name,
                IsActive = model.IsActive,
                CreatedBy = userName
            };
            user.PasswordHash = passwordHasher.HashPassword(user, string.IsNullOrWhiteSpace(model.Password) ? "ChangeMe@123" : model.Password);
            await userRepository.AddAsync(user);
            return ServiceResult.Success("User created successfully.");
        }

        var existing = await userRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("User not found.");
        }

        existing.FullName = model.FullName.Trim();
        existing.Username = model.Username.Trim();
        existing.Email = model.Email.Trim();
        existing.RoleId = role.Id;
        existing.RoleName = role.Name;
        existing.IsActive = model.IsActive;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            existing.PasswordHash = passwordHasher.HashPassword(existing, model.Password);
        }
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await userRepository.UpdateAsync(existing);
        return ServiceResult.Success("User updated successfully.");
    }

    public async Task<ServiceResult> ToggleStatusAsync(string id, string userName)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Failure("User not found.");
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userName;
        await userRepository.UpdateAsync(user);
        return ServiceResult.Success("User status updated.");
    }

    public async Task<ServiceResult> ResetPasswordAsync(string id, string newPassword, string userName)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Failure("User not found.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userName;
        await userRepository.UpdateAsync(user);
        return ServiceResult.Success("Password reset successfully.");
    }

    public async Task<List<LookupOptionViewModel>> GetCashierLookupAsync() =>
        (await userRepository.GetAllAsync(x => x.IsActive && (x.RoleName == "Cashier" || x.RoleName == "Admin")))
        .OrderBy(x => x.FullName)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.FullName })
        .ToList();
}
