using System;

namespace WarehouseApp
{
    public class Warehouse
    {
        private readonly Product[] _products;
        private int _count;

        private readonly string[] _history;
        private int _historyCount;

        public Warehouse(string name, int capacity, int historyCapacity = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название склада не может быть пустым.");
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Ёмкость склада должна быть больше нуля.");
            if (historyCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(historyCapacity), "Ёмкость журнала должна быть больше нуля.");

            Name = name.Trim();
            _products = new Product[capacity];
            _count = 0;

            _history = new string[historyCapacity];
            _historyCount = 0;
        }

        public string Name { get; }
        public int Count { get { return _count; } }
        public int Capacity { get { return _products.Length; } }
        public bool IsFull { get { return _count == _products.Length; } }

        public void Add(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (IsFull)
                throw new InvalidOperationException("Склад заполнен.");
            if (Find(product.Sku) != null)
                throw new InvalidOperationException($"Товар с артикулом {product.Sku} уже есть на складе.");

            _products[_count] = product;
            _count++;
            AddRecord($"Добавлена позиция: {product.Sku} ({product.Name}), остаток: {product.Quantity} шт.");
        }

        public void Remove(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("Артикул не может быть пустым.", nameof(sku));

            string normalizedSku = sku.Trim().ToUpper();
            int index = -1;
            for (int i = 0; i < _count; i++)
            {
                if (_products[i].Sku == normalizedSku)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                throw new InvalidOperationException($"Товар с артикулом {normalizedSku} не найден на складе.");

            string removedName = _products[index].Name;
            for (int i = index; i < _count - 1; i++)
            {
                _products[i] = _products[i + 1];
            }

            _count--;
            _products[_count] = null;
            AddRecord($"Удалена позиция: {normalizedSku} ({removedName}).");
        }

        public Product Find(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            string normalizedSku = sku.Trim().ToUpper();
            for (int i = 0; i < _count; i++)
            {
                if (_products[i].Sku == normalizedSku)
                    return _products[i];
            }
            return null;
        }

        public void ReceiveStock(string sku, int amount)
        {
            Product product = GetExistingProduct(sku);
            product.AddStock(amount);
            AddRecord($"Приёмка: {product.Sku} +{amount} шт. (остаток: {product.Quantity} шт.)");
        }

        public void ShipStock(string sku, int amount)
        {
            Product product = GetExistingProduct(sku);
            product.RemoveStock(amount);
            AddRecord($"Отгрузка: {product.Sku} -{amount} шт. (остаток: {product.Quantity} шт.)");
        }

        public void WriteOffProduct(string sku, int amount, string reason)
        {
            Product product = GetExistingProduct(sku);
            product.WriteOff(amount, reason);
            AddRecord($"Списание: {product.Sku} -{amount} шт. Причина: {reason}");
        }

        public void Transfer(Warehouse destination, string sku, int amount)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "Склад-получатель не указан.");
            if (ReferenceEquals(this, destination))
                throw new ArgumentException("Нельзя переместить товар на тот же самый склад.");
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("Артикул не может быть пустым.", nameof(sku));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Количество для перемещения должно быть больше нуля.");

            Product sourceProduct = Find(sku);
            if (sourceProduct == null)
                throw new InvalidOperationException($"Товар {sku.Trim().ToUpper()} отсутствует на складе «{Name}».");
            if (amount > sourceProduct.Quantity)
                throw new InvalidOperationException($"Недостаточно товара на складе «{Name}»: есть {sourceProduct.Quantity}, запрошено {amount}.");

            Product destProduct = destination.Find(sourceProduct.Sku);
            if (destProduct == null && destination.IsFull)
                throw new InvalidOperationException($"На складе-получателе «{destination.Name}» нет свободных ячеек.");

            sourceProduct.RemoveStock(amount);
            if (destProduct != null)
            {
                destProduct.AddStock(amount);
            }
            else
            {
                Product transferredCopy = new Product(
                    sourceProduct.Sku,
                    sourceProduct.Name,
                    sourceProduct.Price,
                    amount,
                    sourceProduct.MinStock
                );
                if (sourceProduct.Supplier != null)
                    transferredCopy.AssignSupplier(sourceProduct.Supplier);

                destination.Add(transferredCopy);
            }

            AddRecord($"Перемещение на «{destination.Name}»: {sourceProduct.Sku} -{amount} шт.");
            destination.AddRecord($"Перемещение со склада «{Name}»: {sourceProduct.Sku} +{amount} шт.");
        }

        public decimal GetTotalValue()
        {
            decimal total = 0;
            for (int i = 0; i < _count; i++)
                total += _products[i].TotalPrice;
            return total;
        }

        public int GetTotalQuantity()
        {
            int totalQty = 0;
            for (int i = 0; i < _count; i++)
                totalQty += _products[i].Quantity;
            return totalQty;
        }

        public Product[] GetLowStockProducts()
        {
            int lowCount = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_products[i].IsBelowMinStock)
                    lowCount++;
            }

            Product[] lowProducts = new Product[lowCount];
            int index = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_products[i].IsBelowMinStock)
                {
                    lowProducts[index] = _products[i];
                    index++;
                }
            }
            return lowProducts;
        }

        public Product[] GetProducts()
        {
            Product[] copy = new Product[_count];
            for (int i = 0; i < _count; i++)
                copy[i] = _products[i];
            return copy;
        }
        public string[] GetHistory()
        {
            string[] copy = new string[_historyCount];
            for (int i = 0; i < _historyCount; i++)
                copy[i] = _history[i];
            return copy;
        }

        private Product GetExistingProduct(string sku)
        {
            Product product = Find(sku);
            if (product == null)
                throw new InvalidOperationException($"Товар с артикулом {sku} не найден на складе.");
            return product;
        }

        private void AddRecord(string record)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {record}";
            if (_historyCount == _history.Length)
            {
                for (int i = 0; i < _history.Length - 1; i++)
                    _history[i] = _history[i + 1];
                _history[_history.Length - 1] = entry;
            }
            else
            {
                _history[_historyCount] = entry;
                _historyCount++;
            }
        }
    }
}
