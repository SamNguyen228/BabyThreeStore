﻿using System;
using System.Collections.Generic;

namespace WebsiteBaby3.Models;

public partial class Cart
{
    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int Quantity { get; set; }

    public int CartId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
