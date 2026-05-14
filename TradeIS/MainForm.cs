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
            InitTradePointTypes();

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
                cbSupplySupplier, cbReports, cbProductCategory // и остальные, если пропустил
            };

            foreach (var cb in allComboBoxes)
            {
                // Для всех ставим строгий режим выбора
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Тот самый один, который остается свободным для ввода
            cbSupplyProduct.DropDownStyle = ComboBoxStyle.DropDown;

            cbProductCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cbProductUnit.DropDownStyle = ComboBoxStyle.DropDown;

            cbProductCategory.Items.AddRange(new string[]
            {
                "Напитки",
                "Овощи",
                "Фрукты",
                "Молочные",
                "Бытовая техника",
                "Электроника"
            });

            cbProductUnit.Items.AddRange(new string[]
            {
                "шт",
                "кг",
                "л",
                "м",
                "упак"
            });

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

            cbReportFilter.Items.Add("По товару");
            cbReportFilter.Items.Add("По виду товара");
            cbReportFilter.SelectedIndex = 0;

            RefreshComboSources();
        }
        private void InitializeGrids()
        {
            RefreshTradePointsGrid();
            RefreshProductsGrid();
            RefreshSuppliersGrid();
            RefreshSuppliesGrid();
            RefreshSellersGrid();
            RefreshCustomersGrid();
            RefreshSalesGrid();

            RefreshRequestsGrid();
            RefreshOrdersGrid();
        }
        private void InitTradePointTypes()
        {
            cbType.Items.Clear();
            // Эти строки должны СТРОГО совпадать с тем, что возвращает GetPointType() в классах
            cbType.Items.AddRange(new string[]
            {
                "Универмаг",
                "Магазин",
                "Киоск",
                "Лоток"
            });

            // Ставим режим DropDownList, как мы договаривались ранее
            cbType.DropDownStyle = ComboBoxStyle.DropDownList;
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
        private void ConfigureGrid(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoGenerateColumns = true;
        }
        private void BindGrid(DataGridView grid, object data)
        {
            ConfigureGrid(grid);
            grid.DataSource = data;
        }
        private void SetRussianHeaders(DataGridView grid, Dictionary<string, string> headers)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (headers.TryGetValue(col.Name, out var header))
                {
                    col.HeaderText = header;
                }
            }
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

            RefreshCategoryCombo();
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
                    Name = productName.Trim(),
                    Category = "Без категории",
                    Unit = "шт"
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
            if (string.IsNullOrWhiteSpace(tbName.Text)) { MessageBox.Show("Введите название!"); return; }
            if (cbType.SelectedIndex == -1) { MessageBox.Show("Выберите тип точки!"); return; }

            try
            {
                if (_editTradePointId == -1) // РЕЖИМ ДОБАВЛЕНИЯ
                {
                    // Создаем объект нужного класса через фабрику
                    TradePoint newPoint = CreatePointByType(cbType.Text);

                    // Генерируем новый ID
                    newPoint.Id = Program.Store.TradePoints.Count > 0
                        ? Program.Store.TradePoints.Max(p => p.Id) + 1
                        : 1;

                    FillPointData(newPoint);
                    Program.Store.TradePoints.Add(newPoint);
                }
                else // РЕЖИМ ИЗМЕНЕНИЯ
                {
                    var point = Program.Store.TradePoints.FirstOrDefault(p => p.Id == _editTradePointId);
                    if (point != null)
                    {
                        // Если тип изменился, нужно заменить объект (т.к. это разные классы)
                        if (point.GetPointType() != cbType.Text)
                        {
                            int index = Program.Store.TradePoints.IndexOf(point);
                            TradePoint newPoint = CreatePointByType(cbType.Text);
                            newPoint.Id = point.Id;
                            FillPointData(newPoint);
                            Program.Store.TradePoints[index] = newPoint;
                        }
                        else
                        {
                            FillPointData(point);
                        }
                    }
                }

                // Обновляем всё
                RefreshGrid(dgvTradePoints, Program.Store.TradePoints);
                RefreshComboSources(); // Чтобы в отчетах и других вкладках обновились списки
                StorageManager.Save(Program.Store);
                ResetTradePointEditor(); // Очищаем поля и сбрасываем кнопки
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            // 1. ПРОВЕРКА: Если мы в режиме редактирования, кнопка работает как ОТМЕНА
            if (_editTradePointId != -1)
            {
                ResetTradePointEditor(); // Просто очищаем поля и возвращаем кнопку "Добавить"
                return;
            }

            // 2. ЛОГИКА УДАЛЕНИЯ (если не в режиме редактирования)
            if (dgvTradePoints.CurrentRow == null) return;
            var point = (TradePoint)dgvTradePoints.CurrentRow.DataBoundItem;

            var result = MessageBox.Show(
                $"Удаление точки '{point.Name}' удалит всех её продавцов, продажи и заявки. Продолжить?",
                "Внимание",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                // Удаляем связанных продавцов
                var sellersToRemove = Program.Store.Sellers
                    .Where(s => s.TradePointId == point.Id)
                    .ToList();

                foreach (var s in sellersToRemove)
                    Program.Store.Sellers.Remove(s);

                // Удаляем продажи
                var salesToRemove = Program.Store.Sales
                    .Where(s => s.TradePointId == point.Id)
                    .ToList();

                foreach (var s in salesToRemove)
                    Program.Store.Sales.Remove(s);

                // Удаляем заявки
                var requestsToRemove = Program.Store.Requests.Where(r => r.TradePointId == point.Id).ToList();
                foreach (var r in requestsToRemove) Program.Store.Requests.Remove(r);

                // Удаляем саму точку
                Program.Store.TradePoints.Remove(point);

                // Обновляем всё
                RefreshGrid(dgvTradePoints, Program.Store.TradePoints);
                RefreshGrid(dgvSellers, Program.Store.Sellers);
                RefreshGrid(dgvSales, Program.Store.Sales);
                RefreshRequestsGrid();
                RefreshComboSources();
                StorageManager.Save(Program.Store);

                ResetTradePointEditor(); // На всякий случай чистим поля
            }
        }
        private void ResetTradePointEditor()
        {
            _editTradePointId = -1;
            tbName.Clear();
            cbType.SelectedIndex = -1; // Сброс типа вызовет событие и разблокирует numCounters

            numSize.Value = 0;
            numRent.Value = 0;
            numUtilities.Value = 0;

            numCounters.Enabled = true; // Возвращаем доступность по умолчанию
            numCounters.Value = 1;

            btnAddTradePoint.Text = "Добавить";
        }
        private void FillPointData(TradePoint point)
        {
            point.Name = tbName.Text.Trim();
            point.Size = (double)numSize.Value;
            point.Rent = (double)numRent.Value;
            point.Utilities = (double)numUtilities.Value;
            point.Counters = (int)numCounters.Value; // Здесь будет 1 для киосков благодаря нашей "заморозке"
        }
        private TradePoint CreatePointByType(string typeName)
        {
            return typeName switch
            {
                "Универмаг" => new DepartmentStore(),
                "Магазин" => new Shop(),
                "Киоск" => new Kiosk(),
                "Лоток" => new Stall(),
                _ => throw new Exception("Выберите тип торговой точки!")
            };
        }
        private void RefreshTradePointsGrid()
        {
            var data = Program.Store.TradePoints.Select(t => new
            {
                t.Id,
                t.Name,
                t.Size,
                t.Rent,
                t.Utilities,
                t.Counters,
                Type = t.GetPointType()
            }).ToList();

            BindGrid(dgvTradePoints, data);

            SetRussianHeaders(dgvTradePoints, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Name"] = "Название",
                ["Size"] = "Площадь",
                ["Rent"] = "Аренда",
                ["Utilities"] = "Коммунальные услуги",
                ["Counters"] = "Прилавки",
                ["Type"] = "Тип"
            });
        }

        #endregion

        #region Products (Товары)

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            string newName = tbProductName.Text.Trim();
            string category = cbProductCategory.Text.Trim();
            string unit = cbProductUnit.Text.Trim();

            AddValueToComboIfMissing(cbProductCategory, category);
            AddValueToComboIfMissing(cbProductUnit, unit);

            if (!IsValid(!string.IsNullOrWhiteSpace(newName), "Введите название!")) return;

            if (!IsValid(!string.IsNullOrWhiteSpace(category), "Введите категорию!")) return;

            if (!IsValid(!string.IsNullOrWhiteSpace(unit), "Введите единицу измерения!")) return; if (_editProductId == -1)
            {
                // --- РЕЖИМ ДОБАВЛЕНИЯ ---
                if (!IsValid(!Program.Store.Products.Any(p => p.Name == newName), "Такой товар уже есть!")) return;

                AddItem(Program.Store.Products, new Product
                {
                    Id = Program.Store.Counters.ProductId++,
                    Name = newName,
                    Category = category,
                    Unit = unit
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
                    product.Category = category;
                    product.Unit = unit;
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
            cbProductCategory.Text = product.Category;
            cbProductUnit.Text = product.Unit;
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
                var salesToRemove = Program.Store.Sales
                    .Where(s => s.ProductId == product.Id)
                    .ToList();
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

            cbProductCategory.Text = "";
            cbProductUnit.Text = "шт";

            // или:
            // cbProductCategory.SelectedIndex = -1;

            btnAddProduct.Text = "Добавить";
            btnDeleteProduct.Text = "Удалить";

            btnDeleteProduct.UseVisualStyleBackColor = true;
        }

        private void AddValueToComboIfMissing(ComboBox cb, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            bool exists = cb.Items
                .Cast<object>()
                .Any(x => x.ToString().Equals(value,
                    StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                cb.Items.Add(value);
            }
        }
        private void RefreshCategoryCombo()
        {
            cbReportCategory.Items.Clear();

            var categories = Program.Store.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(x => x);

            foreach (var c in categories)
            {
                cbReportCategory.Items.AddRange(
                    categories.Where(c => c != null).ToArray()
                );
            }
        }
        private void RefreshProductsGrid()
        {
            var data = Program.Store.Products.Select(p => new
            {
                p.Id,
                p.Name
            }).ToList();

            BindGrid(dgvProducts, data);

            SetRussianHeaders(dgvProducts, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Name"] = "Товар"
            });
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
                var suppliesToRemove = Program.Store.Supplies
                    .Where(s => s.SupplierId == supplier.Id)
                    .ToList();
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
        private void RefreshSuppliersGrid()
        {
            var data = Program.Store.Suppliers.Select(s => new
            {
                s.Id,
                s.Name
            }).ToList();

            BindGrid(dgvSuppliers, data);

            SetRussianHeaders(dgvSuppliers, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Name"] = "Поставщик"
            });
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
                var tp = (TradePoint)cbSellerTradePoint.SelectedItem;

                var newSeller = new Seller
                {
                    Id = Program.Store.Counters.SellerId++,
                    Name = name,
                    TradePointId = tp.Id,
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
                    seller.Salary = (double)numSalary.Value;

                    int tradePointId = (cbSellerTradePoint.SelectedItem as TradePoint).Id;
                    seller.TradePointId = tradePointId;

                    foreach (var sale in Program.Store.Sales.Where(s => s.SellerId == seller.Id))
                    {
                        sale.TradePointId = tradePointId;
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

            tbSellerName.Text = seller.Name;
            numSalary.Value = (decimal)seller.Salary;

            cbSellerTradePoint.SelectedItem =
                Program.Store.TradePoints.FirstOrDefault(tp => tp.Id == seller.TradePointId);

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
                var salesToRemove = Program.Store.Sales
                    .Where(s => s.SellerId == seller.Id)
                    .ToList();
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
        private void RefreshSellersGrid()
        {
            var data = Program.Store.Sellers.Select(s => new
            {
                s.Id,
                s.Name,
                TradePoint = Lookup.TradePointName(s.TradePointId),
                s.Salary
            }).ToList();

            BindGrid(dgvSellers, data);

            SetRussianHeaders(dgvSellers, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Name"] = "Продавец",
                ["TradePoint"] = "Торговая точка",
                ["Salary"] = "Зарплата"
            });
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
                    customer.Name = name;

                    // ничего в Sales НЕ обновляем, если используем ID
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
            if (dgvCustomers.CurrentRow == null) return;

            var customer = dgvCustomers.CurrentRow.DataBoundItem as Customer;
            if (customer == null) return;

            var dr = MessageBox.Show(
                $"Удалить покупателя {customer.Name}?",
                "Подтверждение",
                MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                var salesToUpdate = Program.Store.Sales
                    .Where(s => s.CustomerId == customer.Id)
                    .ToList();

                foreach (var sale in salesToUpdate)
                {
                    // либо удаляешь продажи:
                    Program.Store.Sales.Remove(sale);

                    // либо оставляешь и меняешь на "аноним" (но тогда нужен CustomerId = 0)
                }

                Program.Store.Customers.Remove(customer);

                RefreshGrid(dgvCustomers, Program.Store.Customers);
                RefreshGrid(dgvSales, Program.Store.Sales);
                StorageManager.Save(Program.Store);
            }
        }
        private void ResetCustomerEditor()
        {
            _editCustomerId = -1;
            tbCustomerName.Clear();

            btnAddCustomer.Text = "Добавить";
            btnDeleteCustomer.Text = "Удалить";
            btnDeleteCustomer.UseVisualStyleBackColor = true;
        }
        private void RefreshCustomersGrid()
        {
            var data = Program.Store.Customers.Select(c => new
            {
                c.Id,
                c.Name
            }).ToList();

            BindGrid(dgvCustomers, data);

            SetRussianHeaders(dgvCustomers, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Name"] = "Покупатель"
            });
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

            var product = cbSaleProduct.SelectedItem as Product;
            var tp = cbSaleTradePoint.SelectedItem as TradePoint;
            var seller = cbSaleSeller.SelectedItem as Seller;

            if (product == null || tp == null || seller == null)
                return;

            var sale = new Sale
            {
                Id = Program.Store.Counters.SaleId++,
                ProductId = product.Id,
                TradePointId = tp.Id,
                SellerId = seller.Id,
                Quantity = (int)numSaleQuantity.Value,
                Price = (double)numSalePrice.Value,
                Date = dtpSaleDate.Value
            };

            if (tp.GetPointType() == "Киоск" || tp.GetPointType() == "Лоток") // если метода нет — убери, ниже скажу
            {
                if (!IsValid(!string.IsNullOrWhiteSpace(cbSaleCustomer.Text),
                    "Для этой точки обязательно указание покупателя!")) return;

                var customer = cbSaleCustomer.SelectedItem as Customer;
                if (customer != null)
                    sale.CustomerId = customer.Id;
            }

            AddItem(Program.Store.Sales, sale, dgvSales);

            DecreaseStock(tp.Id, product.Id, sale.Quantity);
        }
        private void btnDeleteSale_Click(object sender, EventArgs e) => DeleteItem(Program.Store.Sales, dgvSales);
        private void RefreshSalesGrid()
        {
            var data = Program.Store.Sales.Select(s => new
            {
                s.Id,
                Product = Lookup.ProductName(s.ProductId),
                TradePoint = Lookup.TradePointName(s.TradePointId),
                Seller = Lookup.SellerName(s.SellerId),
                Customer = Lookup.CustomerName(s.CustomerId),
                s.Quantity,
                s.Price,
                s.Date
            }).ToList();

            BindGrid(dgvSales, data);

            SetRussianHeaders(dgvSales, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Product"] = "Товар",
                ["TradePoint"] = "Торговая точка",
                ["Seller"] = "Продавец",
                ["Customer"] = "Покупатель",
                ["Quantity"] = "Количество",
                ["Price"] = "Цена",
                ["Date"] = "Дата"
            });
        }
        private void btnAddSupply_Click(object sender, EventArgs e)
        {
            if (!IsValid(cbSupplySupplier.SelectedItem != null, "Выберите поставщика!")) return;
            if (!IsValid(!string.IsNullOrWhiteSpace(cbSupplyProduct.Text), "Выберите или введите товар!")) return;
            if (!IsValid(numSupplyQuantity.Value > 0, "Количество должно быть больше 0!")) return;
            if (!IsValid(numSupplyPrice.Value > 0, "Цена должна быть больше 0!")) return;

            EnsureProductExists(cbSupplyProduct.Text);

            var supplier = cbSupplySupplier.SelectedItem as Supplier;
            if (supplier == null) return;

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Name == cbSupplyProduct.Text);

            if (product == null) return;

            var supply = new Supply
            {
                Id = Program.Store.Counters.SupplyId++,
                SupplierId = supplier.Id,
                ProductId = product.Id,
                Quantity = (int)numSupplyQuantity.Value,
                Price = (double)numSupplyPrice.Value,
                Date = dtpSupplyDate.Value
            };

            AddItem(Program.Store.Supplies, supply, dgvSupplies);

            IncreaseStock(product.Id, supply.Quantity);
        }
        private void btnDeleteSupply_Click(object sender, EventArgs e) => DeleteItem(Program.Store.Supplies, dgvSupplies);
        private void RefreshSuppliesGrid()
        {
            var data = Program.Store.Supplies.Select(s => new
            {
                s.Id,
                Supplier = Lookup.SupplierName(s.SupplierId),
                Product = Lookup.ProductName(s.ProductId),
                s.Quantity,
                s.Price,
                s.Date
            }).ToList();

            BindGrid(dgvSupplies, data);

            SetRussianHeaders(dgvSupplies, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Supplier"] = "Поставщик",
                ["Product"] = "Товар",
                ["Quantity"] = "Количество",
                ["Price"] = "Цена",
                ["Date"] = "Дата"
            });
        }
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
            var data = Program.Store.Requests.Select(r => new
            {
                r.Id,
                TradePoint = Lookup.TradePointName(r.TradePointId),
                Product = Lookup.ProductName(r.ProductId),
                r.Quantity,
                r.Date
            }).ToList();

            BindGrid(dgvRequests, data);

            SetRussianHeaders(dgvRequests, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["TradePoint"] = "Торговая точка",
                ["Product"] = "Товар",
                ["Quantity"] = "Количество",
                ["Date"] = "Дата"
            });
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
            var data = Program.Store.SupplierOrders.Select(o => new
            {
                o.Id,
                Supplier = Lookup.SupplierName(o.SupplierId),
                Product = Lookup.ProductName(o.ProductId),
                o.Quantity,
                o.Price,
                o.Date
            }).ToList();

            BindGrid(dgvOrders, data);

            SetRussianHeaders(dgvOrders, new Dictionary<string, string>
            {
                ["Id"] = "ID",
                ["Supplier"] = "Поставщик",
                ["Product"] = "Товар",
                ["Quantity"] = "Количество",
                ["Price"] = "Цена",
                ["Date"] = "Дата"
            });
        }
        #endregion

        #region Reports & Filtering

        private void btnShowReport_Click(object sender, EventArgs e)
        {
            if (cbReport.SelectedItem == null) return;

            string name = cbReport.SelectedItem.ToString();

            string productName = cbProduct.Text;
            string sellerName = cbSeller.Text;
            string tpName = cbTradePoint.Text;
            string supplierName = cbSupplier.Text;

            DateTime from = dtFrom.Value;
            DateTime to = dtTo.Value;
            int quantity = (int)numQuantity.Value;

            DataTable result = new DataTable();

            try
            {
                switch (name)
                {
                    case "Поставщики товара":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id;

                            var supplierId = Program.Store.Suppliers
                                .FirstOrDefault(s => s.Name == supplierName)?.Id;

                            result = _reportEngine.GetSuppliersByProduct(
                                supplierId,
                                productId,
                                from,
                                to
                            );

                            break;
                        }

                    case "Покупатели товара":
                    case "Покупатели товара по точкам":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id ?? 0;

                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id;

                            result = _reportEngine.GetCustomersByProduct(
                                productId,
                                from,
                                to,
                                null,
                                quantity
                            );
                            break;
                        }

                    case "Товары в торговой точке":
                        {
                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id ?? 0;

                            result = _reportEngine.GetProductsInPoint(tpId);
                            break;
                        }

                    case "Цены товара по точкам":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id ?? 0;

                            result = _reportEngine.GetProductPricesByPoints(
                                productId,
                                null
                            );
                            break;
                        }

                    case "Выработка продавцов":
                    case "Зарплата продавцов":
                        {
                            result = _reportEngine.GetSellersProductivity(
                                from,
                                to,
                                null
                            );
                            break;
                        }

                    case "Выработка конкретного продавца":
                        {
                            var sellerId = Program.Store.Sellers
                                .FirstOrDefault(s => s.Name == sellerName)?.Id ?? 0;

                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id ?? 0;

                            result = _reportEngine.GetSpecificSellerProductivity(
                                sellerId,
                                tpId,
                                from,
                                to
                            );
                            break;
                        }

                    case "Продажи товара":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id ?? 0;

                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id;

                            result = _reportEngine.GetProductSalesReport(
                                productId,
                                from,
                                to,
                                tpId
                            );
                            break;
                        }

                    case "Поставки поставщика":
                        {
                            var supplierId = Program.Store.Suppliers
                                .FirstOrDefault(s => s.Name == supplierName)?.Id ?? 0;

                            result = _reportEngine.GetSuppliesBySupplier(
                                supplierId,
                                from,
                                to
                            );
                            break;
                        }

                    case "Эффективность торговых точек":
                    case "Рентабельность точки":
                        {
                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id ?? 0;

                            result = _reportEngine.GetProfitabilityReport(
                                tpId,
                                from,
                                to
                            );
                            break;
                        }

                    case "Товарооборот":
                        {
                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id;

                            result = _reportEngine.GetTradeTurnover(
                                from,
                                to,
                                tpId
                            );
                            break;
                        }
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
                        lblReportTradePoint,
                        cbTradePoint,

                        lblReportCategory,
                        cbReportCategory);

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
            // 1. Проверяем, какой тип выбрал пользователь
            string selectedType = cbType.Text;
            bool isSmallPoint = (selectedType == "Киоск" || selectedType == "Лоток");

            if (isSmallPoint)
            {
                // Если это киоск/лоток — жестко ставим 1 и выключаем поле
                numCounters.Value = 1;
                numCounters.Enabled = false;
            }
            else
            {
                // Если передумали и выбрали Магазин — даем возможность редактировать прилавки
                numCounters.Enabled = true;

                // Опционально: если там была 1, можно оставить, 
                // чтобы пользователь сам ввел нужное число для магазина
            }
        }
        private void cbReportFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool byCategory = cbReportFilter.Text == "По виду товара";

            numQuantity.Visible = !byCategory;
            lblReportQuantity.Visible = !byCategory;

            if (byCategory)
                numQuantity.Value = 0;
        }
        private void ShowFilters(params Control[] ctrls) { HideAllFilters(); foreach (var c in ctrls) c.Visible = true; }
        private void HideAllFilters()
        {
            Control[] f = { lblReportProduct, cbProduct, lblReportCustomer, cbCustomer, lblReportSupplier, cbSupplier,
                           lblReportSeller, cbSeller, lblReportTradePoint, cbTradePoint, lblReportCategory, cbReportCategory, lblReportDateFrom, dtFrom,
                           lblReportDateTo, dtTo, lblReportQuantity, numQuantity };
            foreach (var c in f) c.Visible = false;
        }

        private void DecreaseStock(int tradePointId, int productId, int quantity)
        {
            var stock = Program.Store.Stocks
                .FirstOrDefault(s =>
                    s.TradePointId == tradePointId &&
                    s.ProductId == productId);

            if (stock == null)
                return;

            if (stock.Quantity < quantity)
            {
                MessageBox.Show("Недостаточно товара на остатке!");
                return;
            }

            stock.Quantity -= quantity;

            if (stock.Quantity < 0)
                stock.Quantity = 0;

            stock.UpdatedAt = DateTime.Now;

            StorageManager.Save(Program.Store);
        }

        private void IncreaseStock(int productId, int quantity)
        {
            var stock = Program.Store.Stocks
                .FirstOrDefault(s => s.ProductId == productId);

            if (stock == null)
            {
                stock = new Stock
                {
                    ProductId = productId,
                    Quantity = 0,
                    UpdatedAt = DateTime.Now
                };

                Program.Store.Stocks.Add(stock);
            }

            stock.Quantity += quantity;
            stock.UpdatedAt = DateTime.Now;

            StorageManager.Save(Program.Store);
        }

        private void AddTransfer(Transfer t)
        {
            var fromStock = Program.Store.Stocks
                .FirstOrDefault(s =>
                    s.TradePointId == t.FromTradePointId &&
                    s.ProductId == t.ProductId);

            if (fromStock == null || fromStock.Quantity < t.Quantity)
            {
                MessageBox.Show("Недостаточно товара для перемещения");
                return;
            }

            fromStock.Quantity -= t.Quantity;

            var toStock = Program.Store.Stocks
                .FirstOrDefault(s =>
                    s.TradePointId == t.ToTradePointId &&
                    s.ProductId == t.ProductId);

            if (toStock == null)
            {
                toStock = new Stock
                {
                    TradePointId = t.ToTradePointId,
                    ProductId = t.ProductId,
                    Quantity = 0
                };

                Program.Store.Stocks.Add(toStock);
            }

            toStock.Quantity += t.Quantity;

            Program.Store.Transfers.Add(t);

            StorageManager.Save(Program.Store);
        }

        #endregion

        private void cbReportFilter_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}