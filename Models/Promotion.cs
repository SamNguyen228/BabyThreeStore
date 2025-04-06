using System;
using System.Collections.Generic;

namespace WebsiteBaby3.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = null!;

    public decimal? DiscountPercentage { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
