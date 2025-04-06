using System;
using System.Collections.Generic;

namespace WebsiteBaby3.Models;

public partial class ShippingAddress
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string Address { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
