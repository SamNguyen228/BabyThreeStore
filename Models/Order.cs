using System;
using System.Collections.Generic;

namespace WebsiteBaby3.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public int? PromotionId { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Promotion? Promotion { get; set; }

    public virtual User User { get; set; } = null!;
}
