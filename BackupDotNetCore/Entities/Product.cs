using System;
using System.Collections.Generic;

namespace BackupDotNetCore.Entities;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Details { get; set; } = null!;

    public decimal Cost { get; set; }
}
