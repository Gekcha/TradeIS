using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using TradeIS.Models;

namespace TradeIS
{
    public partial class MainForm : Form
    {
        #region Initialization & Global Settings

        private readonly ReportEngine _reportEngine;
        private int _editProductId = -1;
        private int _editTradePointId = -1;
        private int _editSupplierId = -1;
        private int _editSellerId = -1;
        private int _editCustomerId = -1;
        private int _editRequestId = -1;
        private int _editOrderId = -1;

        public MainForm()
        {
            InitializeComponent();

            SetGlobalNumericLimits(this);

            _reportEngine = new ReportEngine(Program.Store);

            InitializeComboBoxes();
            InitializeGrids();

            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReport.AllowUserToAddRows = false;
            dgvReport.ReadOnly = true;
        }
        private void SetGlobalNumericLimits(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Если нашли числовое поле - выставляем лимиты
                if (ctrl is NumericUpDown nud)
                {
                    nud.Maximum = 100000000; // 100 миллионов хватит для всего
                }

                // Если это контейнер (например, TabControl или Panel), 
                // ищем внутри него рекурсивно
                if (ctrl.HasChildren)
                {
                    SetGlobalNumericLimits(ctrl);
                }
            }
        }
        private void InitializeComboBoxes()
        {
            var allComboBoxes = new List<ComboBox>
            {
                cbType, cbSellerTradePoint, cbOrderSupplier, cbOrderProduct,
                cbRequestProduct, cbRequestTradePoint, cbReport, cbSeller,
                cbTradePoint, cbSupplier, cbProduct, cbCustomer,
                cbSaleSeller, cbSaleTradePoint, cbSaleProduct, cbSaleCustomer,
                cbSupplySupplier, cbReports // и остальные, если пропустил
            };

            foreach (var cb in allComboBoxes)
            {
                // Для всех ставим строгий режим выбора
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Тот самый один, который остается свободным для ввода
            cbSupplyProduct.DropDownStyle = ComboBoxStyle.DropDown;

            cbReport.Items.Clear();
            cbReport.Items.AddRange(new string[]
            {
                "Поставщики товара",
                "Покупатели товара",
                "Товары в торговой точке",
                "Цены товара по точкам",
                "Выработка продавцов",
                "Выработка конкретного продавца",
                "Продажи товара",
                "Зарплата продавцов",
                "Поставки поставщика",
                "Эффективность торговых точек",
                "Поставки по номеру заказа",
                "Покупатели товара по точкам",
                "Активные покупатели",
                "Товарооборот",
                "Рентабельность точки"
            });

            RefreshComboSources();
        }
        private void InitializeGrids()
        {
            SetupGrid(dgvTradePoints, Program.Store.TradePoints);
            SetupGrid(dgvProducts, Program.Store.Products);
            SetupGrid(dgvSuppliers, Program.Store.Suppliers);
            SetupGrid(dgvSupplies, Program.Store.Supplies);
            SetupGrid(dgvSellers, Program.Store.Sellers);
            SetupGrid(dgvCustomers, Program.Store.Customers);
            SetupGrid(dgvSales, Program.Store.Sales);

            // Вместо прямой привязки используем "представления" с названиями
            RefreshRequestsGrid();
            RefreshOrdersGrid();
        }

        #endregion

        #region Core Helpers (Generic Methods)

        private void AddItem<T>(BindingList<T> list, T item, DataGridView grid, bool update = false) { list.Add(item); RefreshGrid(grid, list); if (update) RefreshComboSources(); StorageManager.Save(Program.Store); }
        private void DeleteItem<T>(BindingList<T> list, DataGridView grid, bool update = false) { if (grid.CurrentRow == null) return; list.Remove((T)grid.CurrentRow.DataBoundItem); RefreshGrid(grid, list); if (update) RefreshComboSources(); StorageManager.Save(Program.Store); }
        private bool IsValid(bool condition, string message)
        {
            if (!condition)
            {
                MessageBox.Show(message, "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        private void SetupGrid(DataGridView grid, object source)
        {
            grid.AutoGenerateColumns = true;
            grid.DataSource = source;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
        }
        private void RefreshGrid(DataGridView grid, object source)
        {
            grid.DataSource = null;
            grid.DataSource = source;
        }
        private void dgvResults_DataError(object sender, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; }

        #endregion

        #region UI & ComboBox Helpers

        private void RefreshComboSources()
        {
            // Обновляем списки. Используем ValueMember = "Id" для тех классов, где храним ID.
            UpdateCombo(cbSupplySupplier, Program.Store.Suppliers);
            UpdateCombo(cbSupplyProduct, Program.Store.Products);
            UpdateCombo(cbSellerTradePoint, Program.Store.TradePoints);
            UpdateCombo(cbSaleProduct, Program.Store.Products);
            UpdateCombo(cbSaleTradePoint, Program.Store.TradePoints);
            UpdateCombo(cbSaleSeller, Program.Store.Sellers);
            UpdateCombo(cbSaleCustomer, Program.Store.Customers);

            // Для заявок и заказов используем привязку по ID
            UpdateCombo(cbRequestTradePoint, Program.Store.TradePoints);
            UpdateCombo(cbRequestProduct, Program.Store.Products);
            UpdateCombo(cbOrderSupplier, Program.Store.Suppliers);
            UpdateCombo(cbOrderProduct, Program.Store.Products);

            UpdateCombo(cbProduct, Program.Store.Products);
            UpdateCombo(cbSupplier, Program.Store.Suppliers);
            UpdateCombo(cbTradePoint, Program.Store.TradePoints);
            UpdateCombo(cbSeller, Program.Store.Sellers);
            UpdateCombo(cbCustomer, Program.Store.Customers);
        }
        private void UpdateCombo(ComboBox cb, object data)
        {
            cb.DataSource = null;
            cb.DataSource = data;
            cb.DisplayMember = "Name";
            cb.ValueMember = "Id"; // Позволяет получать SelectedValue как int
        }
        private void EnsureProductExists(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName)) return;

            // Ищем, есть ли уже товар с таким именем (игнорируя регистр)
            bool exists = Program.Store.Products.Any(p =>
                p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                // Если товара нет, создаем новый объект
                var newProduct = new Product
                {
                    Id = Program.Store.Counters.ProductId++,
                    Name = productName.Trim()
                };

                // Добавляем в общий список
                Program.Store.Products.Add(newProduct);

                // Обновляем визуальную таблицу товаров и все выпадающие списки, 
                // так как справочник изменился
                RefreshGrid(dgvProducts, Program.Store.Products);
                RefreshComboSources();

                // Сохраняем изменения в файл
                StorageManager.Save(Program.Store);
            }
        }

        #endregion

        #region Trade Points (Точки)

        private void btnAddTradePoint_Click(object sender, EventArgs e)
        {
            string name = tbName.Text.Trim();
            string selectedType = cbType.Text;

            // 1. БАЗОВАЯ ВАЛИДАЦИЯ
            if (!IsValid(!string.IsNullOrWhiteSpace(name), "Введите название!")) return;
            if (!IsValid(cbType.SelectedIndex != -1, "Выберите тип точки!")) return;

            // 2. ВАЛИДАЦИЯ ЧИСЛОВЫХ ПОКАЗАТЕЛЕЙ (Площадь, Аренда, Налоги и т.д.)
            // Предполагаю названия твоих NumericUpDown, подправь если отличаются:
            if (!IsValid(numSize.Value > 0, "Площадь должна быть больше 0!")) return;
            if (!IsValid(numRent.Value > 0, "Арендная плата должна быть больше 0!")) return;
            if (!IsValid(numUtilities.Value > 0, "Коммунальные услуги должны быть больше 0!")) return;
            if (!IsValid(numCounters.Value > 0, "Количество прилавков должно быть больше 0!")) return;

            // 3. СПЕЦИФИЧЕСКАЯ ЛОГИКА ТИПОВ
            // Если это Киоск или Лоток — прилавков всегда 1
            if (selectedType == "Киоск" || selectedType == "Лоток")
            {
                numCounters.Value = 1;
            }
            else
            {
                // Для остальных типов (например, Магазин) тоже проверим на > 0
                if (!IsValid(numCounters.Value > 0, "Количество прилавков должно быть больше 0!")) return;
            }

            if (_editTradePointId == -1)
            {
                // --- ЛОГИКА ДОБАВЛЕНИЯ ---
                if (!IsValid(!Program.Store.TradePoints.Any(p => p.Name == name), "Такая точка уже есть!")) return;

                TradePoint newPoint = CreatePointByType(selectedType);
                newPoint.Id = Program.Store.Counters.TradePointId++;
                FillPointData(newPoint);

                AddItem(Program.Store.TradePoints, newPoint, dgvTradePoints, true);
            }
            else
            {
                // --- ЛОГИКА РЕДАКТИРОВАНИЯ ---
                var point = Program.Store.TradePoints.FirstOrDefault(p => p.Id == _editTradePointId);
                if (point != null)
                {
                    string oldName = point.Name;

                    if (point.GetPointType() == selectedType)
                    {
                        FillPointData(point);
                    }
                    else
                    {
                        int index = Program.Store.TradePoints.IndexOf(point);
                        TradePoint newPoint = CreatePointByType(selectedType);
                        newPoint.Id = point.Id;
                        FillPointData(newPoint);
                        Program.Store.TradePoints[index] = newPoint;
                    }

                    UpdateTradePointLinks(oldName, name);
                }

                ResetTradePointEditor();
                RefreshGrid(dgvTradePoints, Program.Store.TradePoints);
                RefreshComboSources();
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditTradePoint_Click(object sender, EventArgs e)
        {
            if (dgvTradePoints.CurrentRow == null) return;

            var point = (TradePoint)dgvTradePoints.CurrentRow.DataBoundItem;

            // 1. Запоминаем ID
            _editTradePointId = point.Id;

            // 2. Переносим данные в поля
            tbName.Text = point.Name;
            numSize.Value = (decimal)point.Size;
            numRent.Value = (decimal)point.Rent;
            numUtilities.Value = (decimal)point.Utilities;
            numCounters.Value = point.Counters;

            // Выбираем тип в ComboBox (Универмаг, Магазин и т.д.)
            cbType.Text = point.GetPointType();

            // 3. Трансформируем кнопки
            btnAddTradePoint.Text = "Сохранить";
            btnDeleteTradePoint.Text = "Отмена";
            btnDeleteTradePoint.BackColor = Color.LightGray;
        }
        private void btnDeleteTradePoint_Click(object sender, EventArgs e)
        {
            if (dgvTradePoints.CurrentRow == null) return;
            var point = (TradePoint)dgvTradePoints.CurrentRow.DataBoundItem;

            if (MessageBox.Show($"Удаление точки '{point.Name}' удалит всех её продавцов, продажи и заявки. Продолжить?",
                "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // Удаляем продавцов этой точки
                var sellersToRemove = Program.Store.Sellers.Where(s => s.TradePoint == point.Name).ToList();
                foreach (var s in sellersToRemove) Program.Store.Sellers.Remove(s);

                // Удаляем продажи этой точки
                var salesToRemove = Program.Store.Sales.Where(s => s.TradePoint == point.Name).ToList();
                foreach (var s in salesToRemove) Program.Store.Sales.Remove(s);

                // Удаляем заявки (связь по ID)
                var requestsToRemove = Program.Store.Requests.Where(r => r.TradePointId == point.Id).ToList();
                foreach (var r in requestsToRemove) Program.Store.Requests.Remove(r);

                // Удаляем саму точку
                Program.Store.TradePoints.Remove(point);

                // Синхронизируем интерфейс
                RefreshGrid(dgvTradePoints, Program.Store.TradePoints);
                RefreshGrid(dgvSellers, Program.Store.Sellers);
                RefreshGrid(dgvSales, Program.Store.Sales);
                RefreshRequestsGrid();
                RefreshComboSources();
                StorageManager.Save(Program.Store);
            }
        }
        private void ResetTradePointEditor()
        {
            _editTradePointId = -1;

            // Очистка полей
            tbName.Clear();
            numSize.Value = 0;
            numRent.Value = 0;
            numUtilities.Value = 0;
            numCounters.Value = 0;
            cbType.SelectedIndex = -1;

            // Возврат кнопок
            btnAddTradePoint.Text = "Добавить";
            btnDeleteTradePoint.Text = "Удалить";
            btnDeleteTradePoint.UseVisualStyleBackColor = true;
        }
        private void FillPointData(TradePoint p)
        {
            p.Name = tbName.Text.Trim();
            p.Size = (double)numSize.Value;
            p.Rent = (double)numRent.Value;
            p.Utilities = (double)numUtilities.Value;
            p.Counters = (int)numCounters.Value;
        }
        private TradePoint CreatePointByType(string type) => type switch
        {
            "Универмаг" => new DepartmentStore(),
            "Магазин" => new Shop(),
            "Киоск" => new Kiosk(),
            _ => new Stall()
        };
        private void UpdateTradePointLinks(string oldName, string newName)
        {
            foreach (var s in Program.Store.Sellers.Where(x => x.TradePoint == oldName)) s.TradePoint = newName;
            foreach (var s in Program.Store.Sales.Where(x => x.TradePoint == oldName)) s.TradePoint = newName;
        }

        #endregion

        #region Products (Товары)

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            string newName = tbProductName.Text.Trim();

            if (!IsValid(!string.IsNullOrWhiteSpace(newName), "Введите название!")) return;
            if (_editProductId == -1)
            {
                // --- РЕЖИМ ДОБАВЛЕНИЯ ---
                if (!IsValid(!Program.Store.Products.Any(p => p.Name == newName), "Такой товар уже есть!")) return;

                AddItem(Program.Store.Products, new Product
                {
                    Id = Program.Store.Counters.ProductId++,
                    Name = newName
                }, dgvProducts, true);
            }
            else
            {
                // --- РЕЖИМ РЕДАКТИРОВАНИЯ ---
                // 1. Находим товар в базе по запомненному ID
                var product = Program.Store.Products.FirstOrDefault(p => p.Id == _editProductId);

                if (product != null)
                {
                    string oldName = product.Name;
                    product.Name = newName; // Меняем имя на новое из TextBox

                    // 2. Каскадное обновление (если имя товара используется в других таблицах)
                    UpdateRelatedNames(oldName, newName);
                }

                // 3. СБРОС: возвращаем всё в режим добавления
                _editProductId = -1;
                btnAddProduct.Text = "Добавить";
                tbProductName.Clear();

                // ПОКАЗЫВАЕМ кнопку удаления обратно
                btnDeleteProduct.Visible = true;

                ResetProductEditor(); // Возвращаем кнопки в режим Добавить/Удалить
                RefreshGrid(dgvProducts, Program.Store.Products);
                RefreshComboSources();
                StorageManager.Save(Program.Store);

                MessageBox.Show("Данные обновлены!");
            }
        }
        private void btnEditProduct_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null) return;

            var product = (Product)dgvProducts.CurrentRow.DataBoundItem;

            // 1. Заполняем поля данными
            tbProductName.Text = product.Name;
            _editProductId = product.Id;

            // 2. Трансформируем кнопку Добавить -> Сохранить
            btnAddProduct.Text = "Сохранить";

            // 3. Трансформируем кнопку Удалить -> Отмена
            btnDeleteProduct.Text = "Отмена";
            // Меняем цвет на желтый/серый, чтобы пользователь видел разницу (опционально)
            btnDeleteProduct.BackColor = Color.LightGray;
        }

        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            if (_editProductId != -1)
            {
                // --- ЛОГИКА ОТМЕНЫ ---
                ResetProductEditor();
                return;
            }

            if (dgvProducts.CurrentRow == null) return;

            var product = (Product)dgvProducts.CurrentRow.DataBoundItem;

            var confirm = MessageBox.Show(
                $"Удаление товара '{product.Name}' приведет к удалению всех связанных заявок, заказов и продаж. Продолжить?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                // 1. Удаляем связанные заявки
                var requestsToRemove = Program.Store.Requests.Where(r => r.ProductId == product.Id).ToList();
                foreach (var r in requestsToRemove) Program.Store.Requests.Remove(r);

                // 2. Удаляем связанные заказы поставщикам
                var ordersToRemove = Program.Store.SupplierOrders.Where(o => o.ProductId == product.Id).ToList();
                foreach (var o in ordersToRemove) Program.Store.SupplierOrders.Remove(o);

                // 3. Удаляем продажи (если в Sale товар хранится по имени, удаляем по имени)
                var salesToRemove = Program.Store.Sales.Where(s => s.Product == product.Name).ToList();
                foreach (var s in salesToRemove) Program.Store.Sales.Remove(s);

                // 4. Удаляем сам товар
                Program.Store.Products.Remove(product);

                // 5. Обновляем всё
                RefreshGrid(dgvProducts, Program.Store.Products);
                RefreshRequestsGrid();
                RefreshOrdersGrid();
                RefreshGrid(dgvSales, Program.Store.Sales);

                StorageManager.Save(Program.Store);
            }
        }
        private void ResetProductEditor()
        {
            _editProductId = -1;
            tbProductName.Clear();

            // Возвращаем названия кнопок
            btnAddProduct.Text = "Добавить";
            btnDeleteProduct.Text = "Удалить";

            // Возвращаем цвет (если меняли)
            btnDeleteProduct.UseVisualStyleBackColor = true;
        }
        private void UpdateRelatedNames(string oldName, string newName)
        {
            // Обновляем в продажах (Sales)
            foreach (var sale in Program.Store.Sales.Where(s => s.Product == oldName))
                sale.Product = newName;

            // Обновляем в поставках (Supplies)
            foreach (var supply in Program.Store.Supplies.Where(s => s.Product == oldName))
                supply.Product = newName;
        }

        #endregion

        #region Staff & Partners (Поставщики, Продавцы, Покупатели)

        // Поставщики
        private void btnAddSupplier_Click(object sender, EventArgs e)
        {
            string name = tbSupplierName.Text.Trim();
            if (!IsValid(!string.IsNullOrWhiteSpace(name), "Введите имя поставщика!")) return;

            if (_editSupplierId == -1)
            {
                // РЕЖИМ ДОБАВЛЕНИЯ
                if (!IsValid(!Program.Store.Suppliers.Any(s => s.Name == name), "Такой поставщик уже есть!")) return;

                AddItem(Program.Store.Suppliers, new Supplier
                {
                    Id = Program.Store.Counters.SupplierId++,
                    Name = name
                }, dgvSuppliers, true);
            }
            else
            {
                // РЕЖИМ РЕДАКТИРОВАНИЯ
                var supplier = Program.Store.Suppliers.FirstOrDefault(s => s.Id == _editSupplierId);
                if (supplier != null)
                {
                    string oldName = supplier.Name;
                    supplier.Name = name;

                    // Обновляем имя во всех связанных поставках (Supplies)
                    foreach (var supply in Program.Store.Supplies.Where(s => s.Supplier == oldName))
                    {
                        supply.Supplier = name;
                    }
                }

                ResetSupplierEditor();
                RefreshGrid(dgvSuppliers, Program.Store.Suppliers);
                RefreshOrdersGrid(); // Обновляем заказы, так как там отображается имя
                RefreshGrid(dgvSupplies, Program.Store.Supplies); // Обновляем таблицу поставок
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditSupplier_Click(object sender, EventArgs e)
        {
            if (dgvSuppliers.CurrentRow == null) return;

            var supplier = (Supplier)dgvSuppliers.CurrentRow.DataBoundItem;

            // Запоминаем ID и заполняем поле
            _editSupplierId = supplier.Id;
            tbSupplierName.Text = supplier.Name;

            // Меняем режим кнопок
            btnAddSupplier.Text = "Сохранить";
            btnDeleteSupplier.Text = "Отмена";
            btnDeleteSupplier.BackColor = Color.LightGray;
        }

        private void btnDeleteSupplier_Click(object sender, EventArgs e)
        {
            if (_editSupplierId != -1)
            {
                ResetSupplierEditor();
                return;
            }

            if (dgvSuppliers.CurrentRow == null) return;
            var supplier = (Supplier)dgvSuppliers.CurrentRow.DataBoundItem;

            if (MessageBox.Show($"Удалить поставщика '{supplier.Name}' и все его заказы/поставки?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Удаляем заказы (связь по ID)
                var ordersToRemove = Program.Store.SupplierOrders.Where(o => o.SupplierId == supplier.Id).ToList();
                foreach (var o in ordersToRemove) Program.Store.SupplierOrders.Remove(o);

                // Удаляем фактические поставки (связь по имени)
                var suppliesToRemove = Program.Store.Supplies.Where(s => s.Supplier == supplier.Name).ToList();
                foreach (var s in suppliesToRemove) Program.Store.Supplies.Remove(s);

                Program.Store.Suppliers.Remove(supplier);

                RefreshGrid(dgvSuppliers, Program.Store.Suppliers);
                RefreshOrdersGrid();
                RefreshGrid(dgvSupplies, Program.Store.Supplies);
                RefreshComboSources();
                StorageManager.Save(Program.Store);
            }
        }
        private void ResetSupplierEditor()
        {
            _editSupplierId = -1;
            tbSupplierName.Clear();

            btnAddSupplier.Text = "Добавить";
            btnDeleteSupplier.Text = "Удалить";
            btnDeleteSupplier.UseVisualStyleBackColor = true;
        }

        // Продавцы
        private void btnAddSeller_Click(object sender, EventArgs e)
        {
            string name = tbSellerName.Text.Trim();
            if (!IsValid(!string.IsNullOrWhiteSpace(name), "Введите имя продавца!")) return;
            if (!IsValid(cbSellerTradePoint.SelectedItem != null, "Выберите торговую точку!")) return;
            if (!IsValid(numSalary.Value > 0, "Зарплата должна быть больше нуля!")) return;

            if (_editSellerId == -1)
            {
                // РЕЖИМ ДОБАВЛЕНИЯ
                var newSeller = new Seller
                {
                    Id = Program.Store.Counters.SellerId++,
                    Name = name,
                    TradePoint = cbSellerTradePoint.Text,
                    Salary = (double)numSalary.Value
                };
                AddItem(Program.Store.Sellers, newSeller, dgvSellers, true);
            }
            else
            {
                // РЕЖИМ РЕДАКТИРОВАНИЯ
                var seller = Program.Store.Sellers.FirstOrDefault(s => s.Id == _editSellerId);
                if (seller != null)
                {
                    string oldName = seller.Name;

                    seller.Name = name;
                    seller.TradePoint = cbSellerTradePoint.Text;
                    seller.Salary = (double)numSalary.Value;

                    // Каскадное обновление: ищем продажи этого продавца и меняем имя
                    foreach (var sale in Program.Store.Sales.Where(s => s.Seller == oldName))
                    {
                        sale.Seller = name;
                        // Если точка у продавца тоже сменилась, обновляем её и в продаже
                        sale.TradePoint = seller.TradePoint;
                    }
                }

                ResetSellerEditor();
                RefreshGrid(dgvSellers, Program.Store.Sellers);
                RefreshGrid(dgvSales, Program.Store.Sales); // Обновляем таблицу продаж
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditSeller_Click(object sender, EventArgs e)
        {
            if (dgvSellers.CurrentRow == null) return;

            var seller = (Seller)dgvSellers.CurrentRow.DataBoundItem;
            _editSellerId = seller.Id;

            // Заполняем поля
            tbSellerName.Text = seller.Name;
            numSalary.Value = (decimal)seller.Salary;

            // Устанавливаем точку в ComboBox. 
            // Важно: точка в продавце хранится как string (имя), поэтому ищем по тексту
            cbSellerTradePoint.Text = seller.TradePoint;

            // Меняем кнопки
            btnAddSeller.Text = "Сохранить";
            btnDeleteSeller.Text = "Отмена";
            btnDeleteSeller.BackColor = Color.LightGray;
        }
        private void btnDeleteSeller_Click(object sender, EventArgs e)
        {
            if (_editSellerId != -1)
            {
                ResetSellerEditor();
                return;
            }

            if (dgvSellers.CurrentRow == null) return;
            var seller = (Seller)dgvSellers.CurrentRow.DataBoundItem;

            if (MessageBox.Show($"Удалить продавца '{seller.Name}'? Все его продажи также будут удалены.",
                "Удаление", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var salesToRemove = Program.Store.Sales.Where(s => s.Seller == seller.Name).ToList();
                foreach (var s in salesToRemove) Program.Store.Sales.Remove(s);

                Program.Store.Sellers.Remove(seller);

                RefreshGrid(dgvSellers, Program.Store.Sellers);
                RefreshGrid(dgvSales, Program.Store.Sales);
                RefreshComboSources();
                StorageManager.Save(Program.Store);
            }
        }
        private void ResetSellerEditor()
        {
            _editSellerId = -1;
            tbSellerName.Clear();
            numSalary.Value = numSalary.Minimum; // Сброс к минимальной зарплате
            cbSellerTradePoint.SelectedIndex = -1;

            btnAddSeller.Text = "Добавить";
            btnDeleteSeller.Text = "Удалить";
            btnDeleteSeller.UseVisualStyleBackColor = true;
        }

        // Покупатели
        private void btnAddCustomer_Click(object sender, EventArgs e)
        {
            string name = tbCustomerName.Text.Trim();
            if (!IsValid(!string.IsNullOrWhiteSpace(name), "Введите имя покупателя!")) return;

            if (_editCustomerId == -1)
            {
                // РЕЖИМ ДОБАВЛЕНИЯ
                if (!IsValid(!Program.Store.Customers.Any(c => c.Name == name), "Такой покупатель уже есть!")) return;

                AddItem(Program.Store.Customers, new Customer
                {
                    Id = Program.Store.Counters.CustomerId++,
                    Name = name
                }, dgvCustomers, true);
            }
            else
            {
                // РЕЖИМ РЕДАКТИРОВАНИЯ
                var customer = Program.Store.Customers.FirstOrDefault(c => c.Id == _editCustomerId);
                if (customer != null)
                {
                    string oldName = customer.Name;
                    customer.Name = name;

                    // Каскадное обновление в таблице продаж (Sales)
                    foreach (var sale in Program.Store.Sales.Where(s => s.Customer == oldName))
                    {
                        sale.Customer = name;
                    }
                }

                ResetCustomerEditor();
                RefreshGrid(dgvCustomers, Program.Store.Customers);
                RefreshGrid(dgvSales, Program.Store.Sales); // Чтобы в продажах имя тоже обновилось
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditCustomer_Click(object sender, EventArgs e)
        {
            if (dgvCustomers.CurrentRow == null) return;

            var customer = (Customer)dgvCustomers.CurrentRow.DataBoundItem;
            _editCustomerId = customer.Id;

            // Заполняем поле ввода (например, tbCustomerName)
            tbCustomerName.Text = customer.Name;

            // Меняем режим кнопок
            btnAddCustomer.Text = "Сохранить";
            btnDeleteCustomer.Text = "Отмена";
            btnDeleteCustomer.BackColor = Color.LightGray;
        }
        private void btnDeleteCustomer_Click(object sender, EventArgs e)
        {
            if (_editCustomerId != -1)
            {
                ResetCustomerEditor();
                return;
            }

            DeleteItem(Program.Store.Customers, dgvCustomers, true);
        }
        private void ResetCustomerEditor()
        {
            _editCustomerId = -1;
            tbCustomerName.Clear();

            btnAddCustomer.Text = "Добавить";
            btnDeleteCustomer.Text = "Удалить";
            btnDeleteCustomer.UseVisualStyleBackColor = true;
        }

        #endregion

        #region Operations (Продажи и Поставки)

        private void btnAddSale_Click(object sender, EventArgs e)
        {
            if (!IsValid(cbSaleProduct.SelectedItem != null, "Выберите товар!")) return;
            if (!IsValid(cbSaleTradePoint.SelectedItem != null, "Выберите точку!")) return;
            if (!IsValid(cbSaleSeller.SelectedItem != null, "Выберите продавца!")) return;
            if (!IsValid(numSaleQuantity.Value > 0, "Укажите количество!")) return;
            if (!IsValid(numSalePrice.Value > 0, "Укажите цену!")) return;

            var sale = new Sale
            {
                Id = Program.Store.Counters.SaleId++,
                Product = cbSaleProduct.Text,
                TradePoint = cbSaleTradePoint.Text,
                Seller = cbSaleSeller.Text,
                Quantity = (int)numSaleQuantity.Value,
                Price = (double)numSalePrice.Value,
                Date = dtpSaleDate.Value
            };

            var tp = cbSaleTradePoint.SelectedItem as TradePoint;
            if (tp != null && tp.AllowsCustomers())
            {
                if (!IsValid(!string.IsNullOrWhiteSpace(cbSaleCustomer.Text), "Для этой точки обязательно указание покупателя!")) return;
                sale.Customer = cbSaleCustomer.Text;
            }

            AddItem(Program.Store.Sales, sale, dgvSales);
        }
        private void btnDeleteSale_Click(object sender, EventArgs e) => DeleteItem(Program.Store.Sales, dgvSales);

        private void btnAddSupply_Click(object sender, EventArgs e)
        {
            if (!IsValid(cbSupplySupplier.SelectedItem != null, "Выберите поставщика!")) return;
            if (!IsValid(!string.IsNullOrWhiteSpace(cbSupplyProduct.Text), "Выберите или введите товар!")) return;
            if (!IsValid(numSupplyQuantity.Value > 0, "Количество должно быть больше 0!")) return;
            if (!IsValid(numSupplyPrice.Value > 0, "Цена должна быть больше 0!")) return;

            EnsureProductExists(cbSupplyProduct.Text);

            AddItem(Program.Store.Supplies, new Supply
            {
                Id = Program.Store.Counters.SupplyId++,
                Supplier = cbSupplySupplier.Text,
                Product = cbSupplyProduct.Text,
                Quantity = (int)numSupplyQuantity.Value,
                Price = (double)numSupplyPrice.Value,
                Date = dtpSupplyDate.Value
            }, dgvSupplies);
        }
        private void btnDeleteSupply_Click(object sender, EventArgs e) => DeleteItem(Program.Store.Supplies, dgvSupplies);
        #endregion

        #region Requests & Orders (Заявки и Заказы)
        // Заявки
        private void btnAddRequest_Click(object sender, EventArgs e)
        {
            if (!IsValid(cbRequestTradePoint.SelectedValue != null, "Выберите точку!")) return;
            if (!IsValid(cbRequestProduct.SelectedValue != null, "Выберите товар!")) return;
            if (!IsValid(numRequestQuantity.Value > 0, "Количество должно быть больше 0!")) return;


            if (_editRequestId == -1)
            {
                // ДОБАВЛЕНИЕ (твой старый код)
                var newRequest = new Request
                {
                    Id = Program.Store.Counters.RequestId++,
                    TradePointId = (int)cbRequestTradePoint.SelectedValue,
                    ProductId = (int)cbRequestProduct.SelectedValue,
                    Quantity = (int)numRequestQuantity.Value,
                    Date = DateTime.Now
                };
                Program.Store.Requests.Add(newRequest);
            }
            else
            {
                // РЕДАКТИРОВАНИЕ
                var request = Program.Store.Requests.FirstOrDefault(r => r.Id == _editRequestId);
                if (request != null)
                {
                    request.TradePointId = (int)cbRequestTradePoint.SelectedValue;
                    request.ProductId = (int)cbRequestProduct.SelectedValue;
                    request.Quantity = (int)numRequestQuantity.Value;
                }
                ResetRequestEditor();
            }

            RefreshRequestsGrid(); // Обновляем таблицу с названиями
            StorageManager.Save(Program.Store);
        }
        private void btnEditRequest_Click(object sender, EventArgs e)
        {
            if (dgvRequests.CurrentRow == null) return;

            // Получаем ID заявки из первой колонки таблицы (которую мы назвали "ID" в GetRequestsView)
            int id = (int)dgvRequests.CurrentRow.Cells["ID"].Value;
            var request = Program.Store.Requests.FirstOrDefault(r => r.Id == id);

            if (request != null)
            {
                _editRequestId = request.Id;

                // Устанавливаем значения в контролах по ID
                cbRequestTradePoint.SelectedValue = request.TradePointId;
                cbRequestProduct.SelectedValue = request.ProductId;
                numRequestQuantity.Value = (decimal)request.Quantity;

                btnAddRequest.Text = "Сохранить";
                btnDeleteRequest.Text = "Отмена";
            }
        }
        private void btnDeleteRequest_Click(object sender, EventArgs e)
        {
            // 1. Сначала проверяем режим ОТМЕНЫ (то, что ты просил)
            if (_editRequestId != -1)
            {
                ResetRequestEditor();
                return;
            }

            // 2. Логика УДАЛЕНИЯ
            if (dgvRequests.CurrentRow == null) return;

            // Извлекаем ID из ячейки DataTable
            int id = (int)dgvRequests.CurrentRow.Cells["ID"].Value;

            if (MessageBox.Show("Удалить выбранную заявку?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // Ищем объект в оригинальном списке по ID
                var request = Program.Store.Requests.FirstOrDefault(r => r.Id == id);

                if (request != null)
                {
                    Program.Store.Requests.Remove(request);

                    // Обновляем визуальную таблицу через ReportEngine
                    RefreshRequestsGrid();

                    // Сохраняем
                    StorageManager.Save(Program.Store);
                }
            }
        }
        private void ResetRequestEditor()
        {
            _editRequestId = -1;
            cbRequestTradePoint.SelectedIndex = -1;
            cbRequestProduct.SelectedIndex = -1;
            numRequestQuantity.Value = 0;
            btnAddRequest.Text = "Добавить";
            btnDeleteRequest.Text = "Удалить";
        }
        private void RefreshRequestsGrid()
        {
            dgvRequests.DataSource = _reportEngine.GetRequestsView();
            // Растягиваем колонки на всю ширину
            dgvRequests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Заказы
        private void btnAddOrder_Click(object sender, EventArgs e)
        {
            // 1. Валидация входных данных
            if (!IsValid(cbOrderSupplier.SelectedValue != null, "Выберите поставщика!")) return;
            if (!IsValid(cbOrderProduct.SelectedValue != null, "Выберите товар!")) return;
            if (!IsValid(numOrderQuantity.Value > 0, "Количество должно быть больше 0!")) return;
            if (!IsValid(numOrderPrice.Value > 0, "Укажите цену заказа!")) return;

            if (_editOrderId == -1)
            {
                // --- РЕЖИМ ДОБАВЛЕНИЯ ---
                var newOrder = new SupplierOrder
                {
                    Id = Program.Store.Counters.OrderId++,
                    SupplierId = (int)cbOrderSupplier.SelectedValue,
                    ProductId = (int)cbOrderProduct.SelectedValue,
                    Quantity = (int)numOrderQuantity.Value,
                    Price = (double)numOrderPrice.Value,
                    Date = DateTime.Now
                };

                // Используем твой стандартный метод добавления в BindingList
                AddItem(Program.Store.SupplierOrders, newOrder, dgvOrders);
            }
            else
            {
                // --- РЕЖИМ РЕДАКТИРОВАНИЯ ---
                // Ищем существующий заказ по сохраненному ID
                var order = Program.Store.SupplierOrders.FirstOrDefault(o => o.Id == _editOrderId);

                if (order != null)
                {
                    order.SupplierId = (int)cbOrderSupplier.SelectedValue;
                    order.ProductId = (int)cbOrderProduct.SelectedValue;
                    order.Quantity = (int)numOrderQuantity.Value;
                    order.Price = (double)numOrderPrice.Value;
                    // Дату можно либо оставить прежней, либо обновить:
                    // order.Date = DateTime.Now; 
                }

                // Возвращаем кнопки и поля в исходное состояние
                ResetOrderEditor();
            }

            // 2. Обновляем визуальную таблицу (так как она использует DataTable из ReportEngine)
            RefreshOrdersGrid();

            // 3. Сохраняем изменения в JSON файл
            StorageManager.Save(Program.Store);
        }
        private void btnEditOrder_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;

            int id = (int)dgvOrders.CurrentRow.Cells["ID"].Value;
            var order = Program.Store.SupplierOrders.FirstOrDefault(o => o.Id == id);

            if (order != null)
            {
                _editOrderId = order.Id;

                cbOrderSupplier.SelectedValue = order.SupplierId;
                cbOrderProduct.SelectedValue = order.ProductId;
                numOrderQuantity.Value = (decimal)order.Quantity;
                numOrderPrice.Value = (decimal)order.Price;

                btnAddOrder.Text = "Сохранить";
                btnDeleteOrder.Text = "Отмена";
            }
        }
        private void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            // 1. Проверяем режим ОТМЕНЫ
            if (_editOrderId != -1)
            {
                // ВАЖНО: вызываем сброс именно для ЗАКАЗОВ
                ResetOrderEditor();
                return;
            }

            // 2. Логика УДАЛЕНИЯ (с учетом DataTable)
            if (dgvOrders.CurrentRow == null) return;

            // Достаем ID из скрытой колонки "ID", которую создает ReportEngine
            int id = (int)dgvOrders.CurrentRow.Cells["ID"].Value;

            if (MessageBox.Show("Удалить выбранный заказ?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // Ищем объект в списке по ID
                var order = Program.Store.SupplierOrders.FirstOrDefault(o => o.Id == id);

                if (order != null)
                {
                    Program.Store.SupplierOrders.Remove(order);

                    // Перерисовываем таблицу с названиями
                    RefreshOrdersGrid();

                    // Сохраняем изменения в файл
                    StorageManager.Save(Program.Store);
                }
            }
        }
        private void ResetOrderEditor()
        {
            _editOrderId = -1;
            cbOrderSupplier.SelectedIndex = -1;
            cbOrderProduct.SelectedIndex = -1;
            numOrderQuantity.Value = 0;
            numOrderPrice.Value = 0;

            btnAddOrder.Text = "Добавить";
            btnDeleteOrder.Text = "Удалить";
            btnDeleteOrder.UseVisualStyleBackColor = true; // Сброс цвета, если менял
        }
        private void RefreshOrdersGrid()
        {
            dgvOrders.DataSource = _reportEngine.GetOrdersView();
            // Растягиваем колонки на всю ширину
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        #endregion

        #region Reports & Filtering

        private void btnShowReport_Click(object sender, EventArgs e)
        {
            if (cbReport.SelectedItem == null) return;

            string name = cbReport.SelectedItem.ToString();

            // Собираем данные с формы (имена совпадают с твоим списком переменных)
            string productName = cbProduct.Text;
            string sellerName = cbSeller.Text;
            string tpName = cbTradePoint.Text;
            string tpType = cbType.Text;
            string supplierName = cbSupplier.Text;

            DateTime from = dtFrom.Value; // Твое название
            DateTime to = dtTo.Value;     // Твое название
            int quantity = (int)numQuantity.Value;

            try
            {
                DataTable result = new DataTable();

                switch (name)
                {
                    case "Поставщики товара":
                        result = _reportEngine.GetSuppliersReport(productName, quantity, from, to);
                        break;
                    case "Покупатели товара":
                    case "Покупатели товара по точкам":
                        result = _reportEngine.GetCustomersByProduct(
                            productName,
                            from,
                            to,
                            tpType,
                            quantity);
                        break;
                    case "Товары в торговой точке":
                        result = _reportEngine.GetProductsInPoint(tpName);
                        break;
                    case "Цены товара по точкам":
                        result = _reportEngine.GetProductPricesByPoints(productName, tpType);
                        break;
                    case "Выработка продавцов":
                    case "Зарплата продавцов":
                        result = _reportEngine.GetSellersProductivity(from, to, tpType);
                        break;
                    case "Выработка конкретного продавца":
                        result = _reportEngine.GetSpecificSellerProductivity(sellerName, tpName, from, to);
                        break;
                    case "Продажи товара":
                        result = _reportEngine.GetProductSalesReport(
                            productName,
                            from,
                            to,
                            tpType,
                            tpName);
                        break;
                    case "Поставки поставщика":
                        result = _reportEngine.GetSuppliersReport(productName, 0, from, to, supplierName);
                        break;
                    case "Эффективность торговых точек":
                    case "Рентабельность точки":
                        var tpObj = Program.Store.TradePoints.FirstOrDefault(x => x.Name == tpName);
                        result = _reportEngine.GetProfitabilityReport(tpObj, from, to);
                        break;
                    case "Поставки по номеру заказа":
                        var allOrders = _reportEngine.GetOrdersView();
                        allOrders.DefaultView.RowFilter = $"ID = {quantity}";
                        result = allOrders.DefaultView.ToTable();
                        break;
                    case "Активные покупатели":
                        result = _reportEngine.GetCustomersByProduct("", from, to, tpType);
                        break;
                    case "Товарооборот":
                        result = _reportEngine.GetTradeTurnover(from, to, tpType);
                        break;
                }

                dgvReport.DataSource = result;
                lblCount.Text = $"Строк: {result.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        private void cbReport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbReport.SelectedItem == null)
                return;

            HideAllFilters();

            string report = cbReport.SelectedItem.ToString();

            switch (report)
            {
                // 1. Поставщики товара
                case "Поставщики товара":

                    ShowFilters(
                        lblReportProduct, cbProduct,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo,
                        lblReportQuantity, numQuantity);

                    break;

                // 2. Покупатели товара
                case "Покупатели товара":

                    ShowFilters(
                        lblReportProduct, cbProduct,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo,
                        lblReportQuantity, numQuantity);

                    break;

                // 3. Товары в торговой точке
                case "Товары в торговой точке":

                    ShowFilters(
                        lblReportTradePoint, cbTradePoint);

                    break;

                // 4. Цены товара по точкам
                case "Цены товара по точкам":

                    ShowFilters(
                        lblReportProduct, cbProduct,
                        lblTypeTP, cbType,
                        lblReportTradePoint, cbTradePoint);

                    break;

                // 5. Выработка продавцов
                case "Выработка продавцов":

                    ShowFilters(
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo,
                        lblTypeTP, cbType);

                    break;

                // 6. Выработка конкретного продавца
                case "Выработка конкретного продавца":

                    ShowFilters(
                        lblReportSeller, cbSeller,
                        lblReportTradePoint, cbTradePoint,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 7. Продажи товара
                case "Продажи товара":

                    ShowFilters(
                        lblReportProduct, cbProduct,
                        lblReportTradePoint, cbTradePoint,
                        lblTypeTP, cbType,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 8. Зарплата продавцов
                case "Зарплата продавцов":

                    ShowFilters(
                        lblTypeTP, cbType);

                    break;

                // 9. Поставки поставщика
                case "Поставки поставщика":

                    ShowFilters(
                        lblReportSupplier, cbSupplier,
                        lblReportProduct, cbProduct,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 10. Эффективность торговых точек
                case "Эффективность торговых точек":

                    ShowFilters(
                        lblReportTradePoint, cbTradePoint,
                        lblTypeTP, cbType,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 11. Рентабельность точки
                case "Рентабельность точки":

                    ShowFilters(
                        lblReportTradePoint, cbTradePoint,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 12. Поставки по номеру заказа
                case "Поставки по номеру заказа":

                    ShowFilters(
                        lblReportQuantity, numQuantity);

                    break;

                // 13. Покупатели товара по точкам
                case "Покупатели товара по точкам":

                    ShowFilters(
                        lblReportProduct, cbProduct,
                        lblTypeTP, cbType,
                        lblReportTradePoint, cbTradePoint,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;

                // 14. Активные покупатели
                case "Активные покупатели":

                    ShowFilters(
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo,
                        lblReportQuantity, numQuantity);

                    break;

                // 15. Товарооборот
                case "Товарооборот":

                    ShowFilters(
                        lblTypeTP, cbType,
                        lblReportTradePoint, cbTradePoint,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);

                    break;
            }
        }
        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Проверяем, что выбрано: Киоск или Лоток
            bool isFixedCounters = cbType.Text == "Киоск" || cbType.Text == "Лоток";

            if (isFixedCounters)
            {
                numCounters.Value = 1;      // Принудительно ставим 1
                numCounters.Enabled = false; // "Замораживаем" поле (оно станет серым)
            }
            else
            {
                numCounters.Enabled = true;  // Размораживаем для магазинов и прочего
            }
        }
        private void ShowFilters(params Control[] ctrls) { HideAllFilters(); foreach (var c in ctrls) c.Visible = true; }
        private void HideAllFilters()
        {
            Control[] f = { lblReportProduct, cbProduct, lblReportCustomer, cbCustomer, lblReportSupplier, cbSupplier,
                           lblReportSeller, cbSeller, lblReportTradePoint, cbTradePoint, lblReportDateFrom, dtFrom,
                           lblReportDateTo, dtTo, lblReportQuantity, numQuantity };
            foreach (var c in f) c.Visible = false;
        }

        #endregion
    }
}