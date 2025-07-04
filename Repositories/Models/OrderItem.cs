﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int CostumeId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<CollaboratorEarning> CollaboratorEarnings { get; set; } = new List<CollaboratorEarning>();

    public virtual Costume Costume { get; set; }

    public virtual Order Order { get; set; }
}