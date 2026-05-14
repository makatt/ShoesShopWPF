using System;
using System.IO;

namespace ShoesShopWPF
{
    public class User
    {
        public int Id { get; set; }
        public string Role { get; set; } = "";
        public string FullName { get; set; } = "";
    }

    public static class CurrentUser
    {
        public static User Instance { get; set; } = null;
    }

    public class Product
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Supplier { get; set; }
        public decimal Price { get; set; }           // ← просто цена из БД
        public int Discount { get; set; }
        public int Stock { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public string Photo { get; set; }

        public bool HasDiscount => Discount > 0;

        // Новое свойство для фона
        public string CardBackground
        {
            get
            {
                if (Stock == 0)
                    return "#A0D8FF";                    // Голубой — нет на складе

                if (Discount >= 15)
                    return "#2EBB57";                    // Зелёный — хорошая скидка

                return "White";                          // Обычный фон
            }
        }

        public string ImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(Photo))
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "picture.png");

                string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", Photo);
                return File.Exists(full) ? full : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "picture.png");
            }
        }

    }
}