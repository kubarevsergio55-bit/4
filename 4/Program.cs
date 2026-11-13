using System;

namespace WarehouseApp
{
    class Program
    {
        static void Main()
        {
            Warehouse eastHub = new Warehouse("ЛЦ «Восточный»", 10);
            Warehouse westHub = new Warehouse("ЛЦ «Западный»", 5);

            Supplier boschSupplier = new Supplier("ООО «ИнструментТрейд»", "+7 495 123-45-67", "7701234567");
            Supplier paintSupplier = new Supplier("АО «СтройКраска»", "+7 812 987-65-43", "780123456789");

            Product drill = new Product("sku-001", "Дрель аккумуляторная", 7490m, 12, minStock: 10);
            Product paint = new Product("sku-002", "Краска фасадная, 10 л", 1890m, 40, minStock: 15);
            Product cable = new Product("sku-003", "Кабель ВВГнг 3х2.5 (бухта)", 5200m, 5, minStock: 3);

            drill.AssignSupplier(boschSupplier);
            paint.AssignSupplier(paintSupplier);

            eastHub.Add(drill);
            eastHub.Add(paint);
            eastHub.Add(cable);

            eastHub.ShipStock("SKU-001", 5); 
            eastHub.ReceiveStock("SKU-002", 10);
            eastHub.WriteOffProduct("SKU-001", 2, "повреждение при транспортировке"); 

            Console.WriteLine($"Всего позиций: {eastHub.Count}, единиц товара: {eastHub.GetTotalQuantity()} шт.");
            Console.WriteLine($"Списано дрелей за всё время: {drill.WrittenOffTotal} шт.");
            Console.WriteLine($"Поставщик дрели: {drill.Supplier.Name} (ИНН: {drill.Supplier.Inn})\n");

            Console.WriteLine("=== Дефицитные позиции ===");
            foreach (Product low in eastHub.GetLowStockProducts())
            {
                Console.WriteLine($"{low.Sku} | {low.Name} | Остаток: {low.Quantity} (мин: {low.MinStock})");
            }

            eastHub.Remove("SKU-003");
            Console.WriteLine($"\nПосле удаления SKU-003 позиций на складе: {eastHub.Count}\n");

            eastHub.Transfer(westHub, "SKU-002", 15);
            Console.WriteLine($"После перемещения: Восточный = {paint.Quantity} шт., Западный = {westHub.Find("SKU-002").Quantity} шт.");

            try
            {
                eastHub.Transfer(westHub, "SKU-001", 999);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Ошибка перемещения (состояние не изменилось): {ex.Message}");
            }

            Console.WriteLine("\n=== Журнал операций ЛЦ «Восточный» ===");
            foreach (string entry in eastHub.GetHistory())
            {
                Console.WriteLine(entry);
            }
        }
    }
}
