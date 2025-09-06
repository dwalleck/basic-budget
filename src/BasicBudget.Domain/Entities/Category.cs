namespace BasicBudget.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemGenerated { get; private set; }
    public string? Color { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Category? ParentCategory { get; private set; }
    private readonly List<Category> _children = new();
    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private readonly List<BudgetCategory> _budgetCategories = new();
    public IReadOnlyCollection<BudgetCategory> BudgetCategories => _budgetCategories.AsReadOnly();

    private Category() { } // EF Core constructor

    public Category(string name, Guid? parentCategoryId = null, string? description = null, string? color = null)
    {
        Id = Guid.NewGuid();
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        ParentCategoryId = parentCategoryId;
        Description = description?.Trim();
        IsSystemGenerated = false;
        Color = ValidateAndNormalizeColor(color);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ValidateBusinessRules();
    }

    // Factory method for system-generated categories
    public static Category CreateSystemGenerated(string name, string? description = null, string? color = null)
    {
        var category = new Category(name, null, description, color);
        category.IsSystemGenerated = true;
        return category;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name cannot be empty", nameof(newName));

        if (newName.Length > 50)
            throw new ArgumentException("Category name cannot exceed 50 characters");

        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateColor(string? newColor)
    {
        Color = ValidateAndNormalizeColor(newColor);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddChild(Category childCategory)
    {
        if (childCategory == null)
            throw new ArgumentNullException(nameof(childCategory));

        if (childCategory.ParentCategoryId != Id)
            throw new ArgumentException("Child category does not belong to this parent");

        if (GetDepth() >= 3)
            throw new InvalidOperationException("Category hierarchy cannot exceed 3 levels");

        if (WouldCreateCircularReference(childCategory))
            throw new InvalidOperationException("Cannot create circular reference in category hierarchy");

        _children.Add(childCategory);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveChild(Category childCategory)
    {
        if (childCategory == null)
            throw new ArgumentNullException(nameof(childCategory));

        _children.Remove(childCategory);
        UpdatedAt = DateTime.UtcNow;
    }

    public int GetDepth()
    {
        var depth = 1;
        var current = ParentCategory;

        while (current != null)
        {
            depth++;
            current = current.ParentCategory;
        }

        return depth;
    }

    public List<Category> GetAllAncestors()
    {
        var ancestors = new List<Category>();
        var current = ParentCategory;

        while (current != null)
        {
            ancestors.Add(current);
            current = current.ParentCategory;
        }

        return ancestors;
    }

    public List<Category> GetAllDescendants()
    {
        var descendants = new List<Category>();

        foreach (var child in _children)
        {
            descendants.Add(child);
            descendants.AddRange(child.GetAllDescendants());
        }

        return descendants;
    }

    public bool IsRoot => ParentCategoryId == null;
    public bool IsLeaf => !_children.Any();
    public int TransactionCount => _transactions.Count;

    public string GetFullPath()
    {
        var path = new List<string>();
        var current = this;

        while (current != null)
        {
            path.Insert(0, current.Name);
            current = current.ParentCategory;
        }

        return string.Join(" > ", path);
    }

    private bool WouldCreateCircularReference(Category childCategory)
    {
        var current = this;

        while (current != null)
        {
            if (current.Id == childCategory.Id)
                return true;
            current = current.ParentCategory;
        }

        return false;
    }

    private static string? ValidateAndNormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        var normalizedColor = color.Trim().ToUpperInvariant();

        if (!normalizedColor.StartsWith("#"))
            normalizedColor = "#" + normalizedColor;

        if (normalizedColor.Length != 7 || !System.Text.RegularExpressions.Regex.IsMatch(normalizedColor, @"^#[0-9A-F]{6}$"))
            throw new ArgumentException("Color must be a valid 6-digit hex code (e.g., #FF0000)");

        return normalizedColor;
    }

    private void ValidateBusinessRules()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Category name is required");

        if (Name.Length > 50)
            throw new ArgumentException("Category name cannot exceed 50 characters");

        if (ParentCategoryId == Id)
            throw new ArgumentException("Category cannot be its own parent");
    }
}