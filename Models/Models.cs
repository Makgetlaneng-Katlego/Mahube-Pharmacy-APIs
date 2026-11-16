using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mahube_Pharmacy.Models;

[Table("users")]
public class User
{
    [Column("user_id")] public int UserId { get; set; }
    [Required, StringLength(255)][Column("fullname")] public string FullName { get; set; } = "";
    [Required, StringLength(255)][Column("email")] public string Email { get; set; } = "";
    [StringLength(255)][Column("phone_number")] public string? PhoneNumber { get; set; }
    [Required, StringLength(255)][Column("password")] public string Password { get; set; } = "";
    [StringLength(255)][Column("password_hash")] public string? PasswordHash { get; set; }
    [Required, StringLength(20)][Column("role")] public string Role { get; set; } = "CUSTOMER";

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

[Table("address")]
public class Address
{
    [Column("address_id")] public int AddressId { get; set; }
    [Column("user_id")] public int? UserId { get; set; }
    [StringLength(50)][Column("address_name")] public string? AddressName { get; set; }
    [StringLength(50)][Column("street_address")] public string? StreetAddress { get; set; }
    [StringLength(50)][Column("city")] public string? City { get; set; }
    [StringLength(50)][Column("province")] public string? Province { get; set; }
    [StringLength(15)][Column("postal_code")] public string? PostalCode { get; set; }
    [StringLength(150)][Column("delivery_instruction")] public string? DeliveryInstruction { get; set; }

    public User? User { get; set; }
}

[Table("products")]
public class Product
{
    [Column("product_id")] public int ProductId { get; set; }
    [Column("category_id")] public int? CategoryId { get; set; }
    [StringLength(150)][Column("product_name")] public string? ProductName { get; set; }
    [StringLength(350)][Column("description")] public string? Description { get; set; }
    [Column("price")] public double? Price { get; set; } // MySQL float maps to double in C#
    [Column("stock_quantity")] public int? StockQuantity { get; set; }
    [StringLength(300)][Column("image")] public string? Image { get; set; }

    [ForeignKey("CategoryId")]
    public ProductCategory? Category { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

[Table("product_categories")]
public class ProductCategory
{
    [Key][Column("category_id")] public int CategoryId { get; set; }
    [StringLength(100)][Column("category_name")] public string? CategoryName { get; set; }
    [StringLength(300)][Column("description")] public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

[Table("orders")]
public class Order
{
    [Key][Column("order_id")] public int OrderId { get; set; }
    [Column("customer_id")] public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")] public User? Customer { get; set; }

    [Column("order_date")] public DateTime? OrderDate { get; set; }
    [Column("total_amount")] public decimal TotalAmount { get; set; }

    [StringLength(50)][Column("status")] public string? Status { get; set; }
    [StringLength(50)][Column("payment_method")] public string? PaymentMethod { get; set; }
    [StringLength(50)][Column("payment_status")] public string? PaymentStatus { get; set; }
    [StringLength(100)][Column("transaction_id")] public string? TransactionId { get; set; }

    [Column("shipping_address")] public int? AddressId { get; set; }
    [ForeignKey("AddressId")] public Address? Address { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();

    [NotMapped] public decimal Subtotal { get; set; }
    [NotMapped] public decimal ServiceFee { get; set; }
    [NotMapped] public string? DeliveryAddressSnapshot { get; set; }
}

[Table("order_items")]
public class OrderItem
{
    [Key][Column("order_item_id")] public int OrderItemId { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    [ForeignKey("OrderId")] public Order? Order { get; set; }

    [Column("product_id")] public int ProductId { get; set; }
    [ForeignKey("ProductId")] public Product? Product { get; set; }

    [Column("quantity")] public int Quantity { get; set; }
    [Column("price_at_time")] public decimal PriceAtTime { get; set; }
}

[Table("payment_logs")]
public class PaymentLog
{
    [Key][Column("log_id")] public int LogId { get; set; }
    [Column("order_id")] public int? OrderId { get; set; }
    public Order? Order { get; set; }
    [Column("response_data")] public string? ResponseData { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}

[Table("prescription")]
public class Prescription
{
    [Key][Column("id")] public int Id { get; set; }
    [Required, StringLength(255)][Column("user_email")] public string UserEmail { get; set; } = "";
    [Required, StringLength(255)][Column("doctor_name")] public string DoctorName { get; set; } = "";
    [Column("date_prescribed")] public DateTime DatePrescribed { get; set; }
    [Required, StringLength(100)][Column("type")] public string Type { get; set; } = "";
    [Column("notes")] public string? Notes { get; set; }
    [Column("file_data")] public byte[]? FileData { get; set; }
    [StringLength(50)][Column("response")] public string? Response { get; set; }
}

[Table("consultation")]
public class Consultation
{
    [Key][Column("consultation_id")] public long ConsultationId { get; set; }
    [StringLength(50)][Column("fullname")] public string? Fullname { get; set; }
    [StringLength(15)][Column("phone_number")] public string? PhoneNumber { get; set; }
    [Column("preferred_date")] public DateTime? PreferredDate { get; set; }
    [StringLength(50)][Column("consultation_type")] public string? ConsultationType { get; set; }
    [StringLength(350)][Column("Description")] public string? Description { get; set; }
    [StringLength(50)][Column("response")] public string? Response { get; set; }
    [StringLength(255)][Column("email")] public string? Email { get; set; }
}