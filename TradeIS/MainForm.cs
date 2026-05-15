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

        private static readonly Dictionary<string, string> TradePointTypeNames = new()
        {
            ["Shop"] = "Магазин",
            ["Kiosk"] = "Киоск",
            ["Stall"] = "Лоток",
            ["DepartmentStore"] = "Универмаг"
        };

        public MainForm()
        {
            InitializeComponent();

            SetGlobalNumericLimits(this);

            _reportEngine = new ReportEngine(Program.Store);

            InitializeComboBoxes();
            InitializeGrids();
            InitTradePointTypes();

            RefreshComboSources();
            LoadProductCategories();
            LoadTradePointTypes();
            LoadCategories();
            LoadTradePointsForReports();

            dtpSaleDate.MaxDate = DateTime.Today;
            dtpSupplyDate.MaxDate = DateTime.Today;

            dtpRequestDate.Visible = false;
            lblRequestDate.Visible = false;

            dtpOrderDate.Visible = false;
            lblOrderDate.Visible = false;

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
                cbSupplySupplier, cbReports, cbProductCategory, cbReportCategory, cbReportFilter,
                cbSupplyTradePoint, cbTradePoint, cbReportTypeTP
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
                "Объём продаж товара",
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
            UpdateCombo(cbSupplyTradePoint, Program.Store.TradePoints);

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
        private void LoadProductCategories()
        {
            cbReportCategory.Items.Clear();

            var categories = Program.Store.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            cbReportCategory.Items.AddRange(categories.ToArray());
        }
        private void LoadTradePointTypes()
        {
            cbReportTypeTP.Items.Clear();

            var types = Program.Store.TradePoints
                .Select(t => t.GetType().Name)
                .Distinct()
                .ToList();

            foreach (var type in types)
            {
                if (TradePointTypeNames.TryGetValue(type, out var ruName))
                    cbReportTypeTP.Items.Add(ruName);
                else
                    cbReportTypeTP.Items.Add(type); // fallback
            }
        }

        private void UpdateCombo(ComboBox cb, object data)
        {
            cb.DataSource = null;

            cb.DisplayMember = "Name";
            cb.ValueMember = "Id";

            cb.DataSource = data;

            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
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
                    LoadTradePointTypes();
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
                RefreshTradePointsGrid();
                RefreshComboSources(); // Чтобы в отчетах и других вкладках обновились списки
                StorageManager.Save(Program.Store);
                ResetTradePointEditor(); // Очищаем поля и сбрасываем кнопки
                LoadTradePointTypes();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void btnEditTradePoint_Click(object sender, EventArgs e)
        {
            if (dgvTradePoints.CurrentRow == null)
                return;

            // Получаем ID из таблицы
            int id = Convert.ToInt32(
                dgvTradePoints.CurrentRow.Cells["Id"].Value
            );

            // Ищем настоящий объект в хранилище
            var point = Program.Store.TradePoints
                .FirstOrDefault(x => x.Id == id);

            if (point == null)
                return;

            // Запоминаем ID редактируемого объекта
            _editTradePointId = point.Id;

            // Заполняем поля
            tbName.Text = point.Name;
            numSize.Value = (decimal)point.Size;
            numRent.Value = (decimal)point.Rent;
            numUtilities.Value = (decimal)point.Utilities;
            numCounters.Value = point.Counters;

            cbType.Text = point.GetPointType();

            // Меняем режим кнопок
            btnAddTradePoint.Text = "Сохранить";
            btnDeleteTradePoint.Text = "Отмена";
            btnDeleteTradePoint.BackColor = Color.LightGray;
        }
        private void btnDeleteTradePoint_Click(object sender, EventArgs e)
        {
            if (_editTradePointId != -1)
            {
                ResetTradePointEditor();
                return;
            }

            if (dgvTradePoints.CurrentRow == null) return;

            int id = (int)dgvTradePoints.CurrentRow.Cells["Id"].Value;

            var point = Program.Store.TradePoints.FirstOrDefault(x => x.Id == id);
            if (point == null) return;

            var result = MessageBox.Show(
                $"Удаление точки '{point.Name}' удалит всех её продавцов, продажи и заявки. Продолжить?",
                "Внимание",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                // Удаляем связанных продавцов
                for (int i = Program.Store.Sellers.Count - 1; i >= 0; i--)
                {
                    if (Program.Store.Sellers[i].TradePointId == point.Id)
                        Program.Store.Sellers.RemoveAt(i);
                }

                // Удаляем продажи
                for (int i = Program.Store.Sales.Count - 1; i >= 0; i--)
                {
                    if (Program.Store.Sales[i].TradePointId == point.Id)
                        Program.Store.Sales.RemoveAt(i);
                }

                // Удаляем заявки
                for (int i = Program.Store.Requests.Count - 1; i >= 0; i--)
                {
                    if (Program.Store.Requests[i].TradePointId == point.Id)
                        Program.Store.Requests.RemoveAt(i);
                }

                // ДОБАВЛЕНО: удаляем поставки (SUPPLIES)
                for (int i = Program.Store.Supplies.Count - 1; i >= 0; i--)
                {
                    if (Program.Store.Supplies[i].TradePointId == point.Id)
                        Program.Store.Supplies.RemoveAt(i);
                }
                // Удаляем саму точку
                Program.Store.TradePoints.Remove(point);

                // Обновляем всё
                RefreshGrid(dgvTradePoints, Program.Store.TradePoints);
                RefreshGrid(dgvSellers, Program.Store.Sellers);
                RefreshGrid(dgvSales, Program.Store.Sales);
                RefreshRequestsGrid();

                RefreshTradePointsGrid();
                RefreshSellersGrid();
                RefreshSalesGrid();
                RefreshRequestsGrid();
                RefreshSuppliesGrid();

                // ДОБАВЛЕНО: обновить ComboBox поставок
                RefreshComboSources();
                LoadTradePointTypes();

                StorageManager.Save(Program.Store);
            }
        }
        private void ResetTradePointEditor()
        {
            _editTradePointId = -1;

            tbName.Clear();
            numSize.Value = 0;
            numRent.Value = 0;
            numUtilities.Value = 0;
            numCounters.Value = 0;

            cbType.SelectedIndex = -1;

            btnAddTradePoint.Text = "Добавить";
            btnDeleteTradePoint.Text = "Удалить";
            btnDeleteTradePoint.BackColor = SystemColors.Control;
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

                LoadCategories();
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
                RefreshProductsGrid();
                RefreshComboSources();
                StorageManager.Save(Program.Store);
                RefreshCategoryCombo();
                MessageBox.Show("Данные обновлены!");
            }
        }
        private void btnEditProduct_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null)
                return;

            // Получаем ID из таблицы
            int id = Convert.ToInt32(
                dgvProducts.CurrentRow.Cells["Id"].Value
            );

            // Ищем настоящий объект
            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return;

            // Заполняем поля
            tbProductName.Text = product.Name;
            cbProductCategory.Text = product.Category;
            cbProductUnit.Text = product.Unit;

            _editProductId = product.Id;

            // Режим редактирования
            btnAddProduct.Text = "Сохранить";

            btnDeleteProduct.Text = "Отмена";
            btnDeleteProduct.BackColor = Color.LightGray;
        }
        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            if (_editProductId != -1)
            {
                ResetProductEditor();
                return;
            }

            if (dgvProducts.CurrentRow == null)
                return;

            int id = Convert.ToInt32(dgvProducts.CurrentRow.Cells["Id"].Value);

            var product = Program.Store.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return;

            var confirm = MessageBox.Show(
                $"Удаление товара '{product.Name}' удалит связанные данные. Продолжить?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            // заявки
            for (int i = Program.Store.Requests.Count - 1; i >= 0; i--)
                if (Program.Store.Requests[i].ProductId == product.Id)
                    Program.Store.Requests.RemoveAt(i);

            // заказы поставщикам
            for (int i = Program.Store.SupplierOrders.Count - 1; i >= 0; i--)
                if (Program.Store.SupplierOrders[i].ProductId == product.Id)
                    Program.Store.SupplierOrders.RemoveAt(i);

            // продажи
            for (int i = Program.Store.Sales.Count - 1; i >= 0; i--)
                if (Program.Store.Sales[i].ProductId == product.Id)
                    Program.Store.Sales.RemoveAt(i);

            // ВОТ ЭТОГО У ТЕБЯ НЕ БЫЛО → поставки
            for (int i = Program.Store.Supplies.Count - 1; i >= 0; i--)
                if (Program.Store.Supplies[i].ProductId == product.Id)
                    Program.Store.Supplies.RemoveAt(i);

            Program.Store.Products.Remove(product);

            RefreshProductsGrid();
            RefreshRequestsGrid();
            RefreshOrdersGrid();
            RefreshSalesGrid();
            RefreshSuppliesGrid();

            StorageManager.Save(Program.Store);

            RefreshCategoryCombo();
            ResetProductEditor();
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
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToArray();

            cbReportCategory.Items.AddRange(categories);
        }
        private void LoadCategories()
        {
            cbProductCategory.Items.Clear();

            var categories = Program.Store.Products
                .Where(p => !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var category in categories)
            {
                cbProductCategory.Items.Add(category);
            }
        }
        private void RefreshProductsGrid()
        {
            dgvProducts.DataSource = null;

            dgvProducts.DataSource = Program.Store.Products.Select(p => new
            {
                ID = p.Id,
                Название = p.Name,
                Категория = p.Category,
                Единица = p.Unit
            }).ToList();

            dgvProducts.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
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
                RefreshSuppliersGrid();
                RefreshOrdersGrid(); // Обновляем заказы, так как там отображается имя
                RefreshSuppliesGrid();
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditSupplier_Click(object sender, EventArgs e)
        {
            if (dgvSuppliers.CurrentRow == null)
                return;

            // Получаем ID из таблицы
            int id = Convert.ToInt32(
                dgvSuppliers.CurrentRow.Cells["Id"].Value
            );

            // Ищем объект
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(s => s.Id == id);

            if (supplier == null)
                return;

            // Заполняем поля
            _editSupplierId = supplier.Id;
            tbSupplierName.Text = supplier.Name;

            // Режим редактирования
            btnAddSupplier.Text = "Сохранить";

            btnDeleteSupplier.Text = "Отмена";
            btnDeleteSupplier.BackColor = Color.LightGray;
        }
        private void btnDeleteSupplier_Click(object sender, EventArgs e)
        {
            // Режим отмены
            if (_editSupplierId != -1)
            {
                ResetSupplierEditor();
                return;
            }

            if (dgvSuppliers.CurrentRow == null)
                return;

            // Получаем ID
            int id = Convert.ToInt32(
                dgvSuppliers.CurrentRow.Cells["Id"].Value
            );

            // Ищем поставщика
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(s => s.Id == id);

            if (supplier == null)
                return;

            var confirm = MessageBox.Show(
                $"Удалить поставщика '{supplier.Name}' и все его заказы/поставки?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
                return;

            // Удаляем заказы
            for (int i = Program.Store.SupplierOrders.Count - 1; i >= 0; i--)
            {
                if (Program.Store.SupplierOrders[i].SupplierId == supplier.Id)
                    Program.Store.SupplierOrders.RemoveAt(i);
            }

            // Удаляем поставки
            for (int i = Program.Store.Supplies.Count - 1; i >= 0; i--)
            {
                if (Program.Store.Supplies[i].SupplierId == supplier.Id)
                    Program.Store.Supplies.RemoveAt(i);
            }

            // Удаляем поставщика
            Program.Store.Suppliers.Remove(supplier);

            // Обновляем таблицы
            RefreshSuppliersGrid();
            RefreshOrdersGrid();
            RefreshSuppliesGrid();
            RefreshComboSources();

            StorageManager.Save(Program.Store);

            ResetSupplierEditor();
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
                RefreshSellersGrid();
                RefreshSalesGrid();
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditSeller_Click(object sender, EventArgs e)
        {
            if (dgvSellers.CurrentRow == null)
                return;

            // Получаем ID из таблицы
            int id = Convert.ToInt32(
                dgvSellers.CurrentRow.Cells["Id"].Value
            );

            // Ищем объект
            var seller = Program.Store.Sellers
                .FirstOrDefault(s => s.Id == id);

            if (seller == null)
                return;

            _editSellerId = seller.Id;

            tbSellerName.Text = seller.Name;
            numSalary.Value = (decimal)seller.Salary;

            cbSellerTradePoint.SelectedItem =
                Program.Store.TradePoints
                    .FirstOrDefault(tp => tp.Id == seller.TradePointId);

            btnAddSeller.Text = "Сохранить";

            btnDeleteSeller.Text = "Отмена";
            btnDeleteSeller.BackColor = Color.LightGray;
        }
        private void btnDeleteSeller_Click(object sender, EventArgs e)
        {
            // Режим отмены
            if (_editSellerId != -1)
            {
                ResetSellerEditor();
                return;
            }

            if (dgvSellers.CurrentRow == null)
                return;

            // Получаем ID
            int id = Convert.ToInt32(
                dgvSellers.CurrentRow.Cells["Id"].Value
            );

            // Ищем продавца
            var seller = Program.Store.Sellers
                .FirstOrDefault(s => s.Id == id);

            if (seller == null)
                return;

            var confirm = MessageBox.Show(
                $"Удалить продавца '{seller.Name}'? Все его продажи также будут удалены.",
                "Удаление",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            // Удаляем продажи
            for (int i = Program.Store.Sales.Count - 1; i >= 0; i--)
            {
                if (Program.Store.Sales[i].SellerId == seller.Id)
                    Program.Store.Sales.RemoveAt(i);
            }

            // Удаляем продавца
            Program.Store.Sellers.Remove(seller);

            // Обновляем таблицы
            RefreshSellersGrid();
            RefreshSalesGrid();
            RefreshComboSources();

            StorageManager.Save(Program.Store);

            ResetSellerEditor();
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
                RefreshCustomersGrid();
                RefreshSalesGrid();
                StorageManager.Save(Program.Store);
            }
        }
        private void btnEditCustomer_Click(object sender, EventArgs e)
        {
            if (dgvCustomers.CurrentRow == null)
                return;

            int id = Convert.ToInt32(
                dgvCustomers.CurrentRow.Cells["Id"].Value
            );

            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
                return;

            _editCustomerId = customer.Id;

            tbCustomerName.Text = customer.Name;

            btnAddCustomer.Text = "Сохранить";

            btnDeleteCustomer.Text = "Отмена";
            btnDeleteCustomer.BackColor = Color.LightGray;
        }
        private void btnDeleteCustomer_Click(object sender, EventArgs e)
        {
            // Режим отмены
            if (_editCustomerId != -1)
            {
                ResetCustomerEditor();
                return;
            }

            if (dgvCustomers.CurrentRow == null)
                return;

            int id = Convert.ToInt32(
                dgvCustomers.CurrentRow.Cells["Id"].Value
            );

            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
                return;

            var dr = MessageBox.Show(
                $"Удалить покупателя {customer.Name}?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (dr != DialogResult.Yes)
                return;

            // Удаляем продажи клиента
            for (int i = Program.Store.Sales.Count - 1; i >= 0; i--)
            {
                if (Program.Store.Sales[i].CustomerId == customer.Id)
                    Program.Store.Sales.RemoveAt(i);
            }

            // Удаляем клиента
            Program.Store.Customers.Remove(customer);

            RefreshCustomersGrid();
            RefreshSalesGrid();

            StorageManager.Save(Program.Store);

            ResetCustomerEditor();
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
            if (cbSaleProduct.SelectedValue == null) return;
            if (cbSaleTradePoint.SelectedValue == null) return;
            if (cbSaleSeller.SelectedValue == null) return;

            if (!IsValid(numSaleQuantity.Value > 0, "Укажите количество!")) return;
            if (!IsValid(numSalePrice.Value > 0, "Укажите цену!")) return;

            if (dtpSaleDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Дата продажи не может быть больше текущей!");
                return;
            }

            int productId = (int)cbSaleProduct.SelectedValue;
            int tradePointId = (int)cbSaleTradePoint.SelectedValue;
            int sellerId = (int)cbSaleSeller.SelectedValue;

            var sale = new Sale
            {
                Id = Program.Store.Counters.SaleId++,
                ProductId = productId,
                TradePointId = tradePointId,
                SellerId = sellerId,
                Quantity = (int)numSaleQuantity.Value,
                Price = (double)numSalePrice.Value,
                Date = dtpSaleDate.Value
            };

            // Проверка клиента только если требуется
            var tp = Program.Store.TradePoints.FirstOrDefault(x => x.Id == tradePointId);

            if (tp != null && (tp.GetPointType() == "Kiosk" || tp.GetPointType() == "Stall"))
            {
                if (cbSaleCustomer.SelectedValue == null)
                {
                    MessageBox.Show("Для этой точки обязательно указание покупателя!");
                    return;
                }

                sale.CustomerId = (int)cbSaleCustomer.SelectedValue;
            }

            AddItem(Program.Store.Sales, sale, dgvSales);

            DecreaseStock(tradePointId, productId, sale.Quantity);

            RefreshSalesGrid();
        }
        private void btnDeleteSale_Click(object sender, EventArgs e)
        {
            if (dgvSales.CurrentRow == null)
                return;

            int id = Convert.ToInt32(dgvSales.CurrentRow.Cells["Id"].Value);

            var sale = Program.Store.Sales.FirstOrDefault(s => s.Id == id);
            if (sale == null)
                return;

            var result = MessageBox.Show(
                "Удалить выбранную продажу?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // 1. возвращаем товар на склад
            var stock = Program.Store.Stocks.FirstOrDefault(s =>
                s.TradePointId == sale.TradePointId &&
                s.ProductId == sale.ProductId);

            if (stock != null)
            {
                stock.Quantity += sale.Quantity;
                stock.UpdatedAt = DateTime.Now;
            }
            else
            {
                Program.Store.Stocks.Add(new Stock
                {
                    TradePointId = sale.TradePointId,
                    ProductId = sale.ProductId,
                    Quantity = sale.Quantity,
                    UpdatedAt = DateTime.Now
                });
            }

            // 2. удаляем продажу
            Program.Store.Sales.Remove(sale);

            // 3. обновляем UI
            RefreshSalesGrid();
            RefreshGrid(dgvProducts, Program.Store.Products);
            RefreshComboSources();

            StorageManager.Save(Program.Store);
        }
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
            if (cbSupplySupplier.SelectedValue == null)
                return;

            if (cbSupplyTradePoint.SelectedValue == null)
            {
                MessageBox.Show("Выберите торговую точку!");
                return;
            }

            if (!IsValid(!string.IsNullOrWhiteSpace(cbSupplyProduct.Text),
                "Выберите или введите товар!"))
                return;

            if (!IsValid(numSupplyQuantity.Value > 0,
                "Количество должно быть больше 0!"))
                return;

            if (!IsValid(numSupplyPrice.Value > 0,
                "Цена должна быть больше 0!"))
                return;

            if (dtpSupplyDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Дата поставки не может быть больше текущей!");
                return;
            }

            EnsureProductExists(cbSupplyProduct.Text);

            int supplierId = (int)cbSupplySupplier.SelectedValue;
            int tradePointId = (int)cbSupplyTradePoint.SelectedValue;

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Name == cbSupplyProduct.Text.Trim());

            if (product == null)
                return;

            var supply = new Supply
            {
                Id = Program.Store.Counters.SupplyId++,
                TradePointId = tradePointId,
                SupplierId = supplierId,
                ProductId = product.Id,
                Quantity = (int)numSupplyQuantity.Value,
                Price = (double)numSupplyPrice.Value,
                Date = dtpSupplyDate.Value
            };

            IncreaseStock(
                tradePointId,
                product.Id,
                supply.Quantity);

            Program.Store.Supplies.Add(supply);

            RefreshSuppliesGrid();

            StorageManager.Save(Program.Store);
        }
        private void btnDeleteSupply_Click(object sender, EventArgs e)
        {
            if (dgvSupplies.CurrentRow == null)
                return;

            int id = Convert.ToInt32(
                dgvSupplies.CurrentRow.Cells["Id"].Value);

            var supply = Program.Store.Supplies
                .FirstOrDefault(s => s.Id == id);

            if (supply == null)
                return;

            var confirm = MessageBox.Show(
                "Удалить поставку? Это изменит остатки на складе.",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            // уменьшаем остаток
            var stock = Program.Store.Stocks
                .FirstOrDefault(s =>
                    s.TradePointId == supply.TradePointId &&
                    s.ProductId == supply.ProductId);

            if (stock != null)
            {
                stock.Quantity -= supply.Quantity;

                if (stock.Quantity <= 0)
                    Program.Store.Stocks.Remove(stock);
            }

            // удаляем поставку
            Program.Store.Supplies.Remove(supply);

            // обновляем UI
            RefreshSuppliesGrid();
            RefreshProductsGrid();

            RefreshComboSources();

            StorageManager.Save(Program.Store);
        }
        private void RefreshSuppliesGrid()
        {
            var data = Program.Store.Supplies.Select(s => new
            {
                s.Id,
                Supplier = Lookup.SupplierName(s.SupplierId),
                Product = Lookup.ProductName(s.ProductId),
                TradePoint = Lookup.TradePointName(s.TradePointId),
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
                ["TradePoint"] = "Торговая точка",
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

            DateTime? from = dtFrom.Checked
                ? dtFrom.Value.Date
                : (DateTime?)null;

            DateTime? to = dtTo.Checked
                ? dtTo.Value.Date.AddDays(1).AddTicks(-1)
                : (DateTime?)null;
            int quantity = (int)numQuantity.Value;

            DataTable result = new DataTable();

            try
            {
                switch (name)
                {
                    case "Поставщики товара":
                        {
                            int minQuantity = (int)numQuantity.Value;
                            if (cbReportFilter.Text == "По виду товара")
                            {
                                var category = cbReportCategory.Text;

                                if (string.IsNullOrWhiteSpace(category))
                                {
                                    result = new DataTable();
                                    break;
                                }

                                result = _reportEngine.GetSuppliersByCategory(
                                    category,
                                    minQuantity,
                                    from,
                                    to
                                );
                            }
                            else
                            {
                                var product = Program.Store.Products
                                    .FirstOrDefault(p => p.Name == cbProduct.Text);

                                if (product == null)
                                {
                                    result = new DataTable();
                                    break;
                                }

                                result = _reportEngine.GetSuppliersByProduct(
                                    product.Id,
                                    minQuantity,
                                    from,
                                    to
                                );
                            }

                            break;
                        }

                    case "Покупатели товара":
                    case "Покупатели товара по точкам":
                        {
                            int minQuantity = (int)numQuantity.Value;

                            if (cbReportFilter.Text == "По виду товара")
                            {
                                var category = cbReportCategory.Text;

                                if (string.IsNullOrWhiteSpace(category))
                                {
                                    result = new DataTable();
                                    break;
                                }

                                result = _reportEngine.GetCustomersByCategory(
                                    category,
                                    from,
                                    to,
                                    minQuantity
                                );
                            }
                            else
                            {
                                var product = Program.Store.Products
                                    .FirstOrDefault(p => p.Name == productName);

                                if (product == null)
                                {
                                    result = new DataTable();
                                    break;
                                }

                                result = _reportEngine.GetCustomersByProduct(
                                    product.Id,
                                    from,
                                    to,
                                    minQuantity
                                );
                            }

                            break;
                        }

                    case "Товары в торговой точке":
                        {
                            var tp = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName);

                            if (tp == null)
                            {
                                result = new DataTable();
                                break;
                            }

                            result = _reportEngine.GetProductsInTradePoint(tp.Id);
                            break;
                        }

                    case "Цены товара по точкам":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id ?? 0;

                            string mode = cbReportFilter.Text;

                            if (mode == "Все торговые точки")
                            {
                                result = _reportEngine.GetProductPricesByPoints(
                                    productId,
                                    null,
                                    null
                                );
                            }
                            else if (mode == "По типу торговой точки")
                            {
                                string type = cbReportTypeTP.Text;

                                result = _reportEngine.GetProductPricesByPointType(
                                    productId,
                                    type
                                );
                            }
                            else if (mode == "По конкретной торговой точке")
                            {
                                var tpId = Program.Store.TradePoints
                                    .FirstOrDefault(t => t.Name == cbTradePoint.Text)?.Id ?? 0;

                                result = _reportEngine.GetProductPricesByPoint(
                                    productId,
                                    tpId
                                );
                            }
                            else
                            {
                                result = new DataTable();
                            }

                            break;
                        }

                    case "Выработка продавцов":
                        {
                            if (cbReportFilter.Text == "По типу торговой точки")
                            {
                                string type = GetTradePointTypeInternal(
                                    cbReportTypeTP.Text);

                                result = _reportEngine
                                    .GetSellersProductivityByType(
                                        from.Value,
                                        to.Value,
                                        type
                                    );
                            }
                            else
                            {
                                result = _reportEngine
                                    .GetSellersProductivity(
                                        from.Value,
                                        to.Value
                                    );
                            }

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
                                from.Value,
                                to.Value
                            );

                            break;
                        }

                    case "Объём продаж товара":
                        {
                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == productName)?.Id ?? 0;

                            string mode = cbReportFilter.Text;

                            if (mode == "По типу торговой точки")
                            {
                                string type = GetTradePointTypeInternal(cbReportTypeTP.Text);

                                result = _reportEngine.GetProductSalesVolumeByType(
                                    productId,
                                    type,
                                    from.Value,
                                    to.Value
                                );
                            }
                            else if (mode == "По конкретной торговой точке")
                            {
                                var tpId = Program.Store.TradePoints
                                    .FirstOrDefault(t => t.Name == cbTradePoint.Text)?.Id ?? 0;

                                result = _reportEngine.GetProductSalesVolumeByPoint(
                                    productId,
                                    tpId,
                                    from.Value,
                                    to.Value
                                );
                            }
                            else
                            {
                                result = _reportEngine.GetProductSalesVolumeAll(
                                    productId,
                                    from.Value,
                                    to.Value
                                );
                            }

                            break;
                        }

                    case "Зарплата продавцов":
                        {
                            string mode = cbReportFilter.Text;

                            if (mode == "По типу торговой точки")
                            {
                                string type = GetTradePointTypeInternal(
                                    cbReportTypeTP.Text);

                                result = _reportEngine.GetSellersSalaryByType(
                                    type,
                                    from.Value,
                                    to.Value
                                );
                            }
                            else if (mode == "По конкретной торговой точке")
                            {
                                int tpId = Program.Store.TradePoints
                                    .FirstOrDefault(t =>
                                        t.Name == cbTradePoint.Text)?.Id ?? 0;

                                result = _reportEngine.GetSellersSalaryByPoint(
                                    tpId,
                                    from.Value,
                                    to.Value
                                );
                            }
                            else
                            {
                                result = _reportEngine.GetSellersSalaryAll(
                                    from.Value,
                                    to.Value
                                );
                            }

                            break;
                        }
                    case "Поставки поставщика":
                        {
                            var supplierId = Program.Store.Suppliers
                                .FirstOrDefault(s => s.Name == cbSupplier.Text)?.Id ?? 0;

                            var productId = Program.Store.Products
                                .FirstOrDefault(p => p.Name == cbProduct.Text)?.Id ?? 0;

                            result = _reportEngine.GetSupplierProductSupplies(
                                supplierId,
                                productId,
                                dtFrom.Value,
                                dtTo.Value
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
                                from.Value,
                                to.Value
                            );
                            break;
                        }

                    case "Товарооборот":
                        {
                            var tpId = Program.Store.TradePoints
                                .FirstOrDefault(t => t.Name == tpName)?.Id;

                            result = _reportEngine.GetTradeTurnover(
                                from.Value,
                                to.Value,
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
        private void UpdateReportFilter(string reportName)
        {
            cbReportFilter.Items.Clear();

            switch (reportName)
            {
                case "Поставщики товара":
                case "Покупатели товара":
                    cbReportFilter.Items.Add("По виду товара");
                    cbReportFilter.Items.Add("По товару");
                    cbReportFilter.SelectedIndex = 0;
                    cbReportCategory.SelectedIndex = 0;
                    break;

                case "Цены товара по точкам":
                    cbReportFilter.Items.Add("Все торговые точки");
                    cbReportFilter.Items.Add("По типу торговой точки");
                    cbReportFilter.Items.Add("По конкретной торговой точке");
                    cbReportFilter.SelectedIndex = 0;
                    cbReportCategory.SelectedIndex = 0;
                    break;
            }
        }
        private void cbReport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbReport.SelectedItem == null)
                return;

            HideAllFilters();
            ResetReportFilter();

            string report = cbReport.SelectedItem.ToString();

            LoadReportFilter(report);

            switch (report)
            {
                case "Поставщики товара":
                case "Покупатели товара":
                    ShowFilters(
                        lblReportFilter, cbReportFilter,
                        lblReportProduct, cbProduct,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo,
                        lblReportQuantity, numQuantity);
                    break;

                case "Цены товара по точкам":
                    ShowFilters(
                        lblReportFilter, cbReportFilter,
                        lblReportProduct, cbProduct,
                        lblReportTypeTP, cbReportTypeTP,
                        lblReportTradePoint, cbTradePoint,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);
                    break;

                case "Объём продаж товара":
                    ShowFilters(
                        lblReportFilter, cbReportFilter,
                        lblReportProduct, cbProduct,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);
                    break;

                case "Выработка продавцов":
                    ShowFilters(
                        lblReportFilter, cbReportFilter,
                        lblReportDateFrom, dtFrom,
                        lblReportDateTo, dtTo);
                    break;
                case "Зарплата продавцов":
                    ShowFilters(
                        lblReportFilter, cbReportFilter);
                    break;
                case "Поставки поставщика":
                    ShowFilters(
                        lblReportSupplier, cbSupplier,
                        lblReportProduct, cbProduct,
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
            string report = cbReport.SelectedItem?.ToString();
            string mode = cbReportFilter.Text;

            if (report == "Поставщики товара" || report == "Покупатели товара")
            {
                bool byCategory = mode == "По виду товара";

                lblReportProduct.Visible = !byCategory;
                cbProduct.Visible = !byCategory;

                lblReportCategory.Visible = byCategory;
                cbReportCategory.Visible = byCategory;

                lblReportQuantity.Visible = !byCategory;
                numQuantity.Visible = !byCategory;

                if (byCategory)
                    numQuantity.Value = 0;

                return;
            }

            if (report == "Цены товара по точкам" || report == "Объём продаж товара")
            {
                lblReportTypeTP.Visible = (mode == "По типу торговой точки");
                cbReportTypeTP.Visible = (mode == "По типу торговой точки");

                lblReportTradePoint.Visible = (mode == "По конкретной торговой точке");
                cbTradePoint.Visible = (mode == "По конкретной торговой точке");

                return;
            }

            if (report == "Выработка продавцов")
            {
                lblReportTypeTP.Visible = (mode == "По типу торговой точки");
                cbReportTypeTP.Visible = (mode == "По типу торговой точки");

                return;
            }
            if (report == "Зарплата продавцов")
            {
                mode = cbReportFilter.Text;

                lblReportTradePoint.Visible = false;
                cbTradePoint.Visible = false;

                lblReportTypeTP.Visible = false;
                cbReportTypeTP.Visible = false;

                if (mode == "По типу торговой точки")
                {
                    lblReportTypeTP.Visible = true;
                    cbReportTypeTP.Visible = true;
                }
                else if (mode == "По конкретной торговой точке")
                {
                    lblReportTradePoint.Visible = true;
                    cbTradePoint.Visible = true;
                }

                return;
            }
        }
        private void cbSaleTradePoint_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tp = cbSaleTradePoint.SelectedItem as TradePoint;

            if (tp == null)
                return;

            bool hideCustomer =
                tp.GetPointType() == "Киоск" ||
                tp.GetPointType() == "Лоток";

            lblSaleCustomer.Visible = !hideCustomer;
            cbSaleCustomer.Visible = !hideCustomer;

            if (hideCustomer)
                cbSaleCustomer.SelectedIndex = -1;
        }

        private string GetTradePointTypeInternal(string russianType)
        {
            switch (russianType)
            {
                case "Магазин":
                    return "Shop";

                case "Киоск":
                    return "Kiosk";

                case "Лоток":
                    return "Stall";

                case "Универмаг":
                    return "DepartmentStore";

                default:
                    return "";
            }
        }
        private void ShowFilters(params Control[] ctrls) { HideAllFilters(); foreach (var c in ctrls) c.Visible = true; }
        private void HideAllFilters()
        {
            Control[] f = { lblReportProduct, cbProduct, lblReportCustomer, cbCustomer, lblReportSupplier, cbSupplier,
                           lblReportSeller, cbSeller, lblReportTradePoint, cbTradePoint, lblReportCategory, cbReportCategory, lblReportDateFrom, dtFrom,
                           lblReportDateTo, dtTo, lblReportQuantity, numQuantity, lblReportCategory, cbReportCategory, lblReportFilter, cbReportFilter,
                            lblReportTypeTP, cbReportTypeTP};
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

        private void IncreaseStock(int tradePointId, int productId, int quantity)
        {
            var stock = Program.Store.Stocks
                .FirstOrDefault(s =>
                    s.TradePointId == tradePointId &&
                    s.ProductId == productId);

            if (stock == null)
            {
                stock = new Stock
                {
                    TradePointId = tradePointId,
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

        private void cbTradePoint_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            cbSeller.DataSource = null;   // ВАЖНО

            cbSeller.Items.Clear();

            var tp = Program.Store.TradePoints
                .FirstOrDefault(t => t.Name == cbTradePoint.Text);

            if (tp == null)
                return;

            var sellers = Program.Store.Sellers
                .Where(s => s.TradePointId == tp.Id)
                .Select(s => s.Name)
                .ToArray();

            cbSeller.Items.AddRange(sellers);
        }

        private void LoadTradePointsForReports()
        {
            var tpIds = Program.Store.Sellers
                .Select(s => s.TradePointId)
                .Distinct();

            var tradePoints = Program.Store.TradePoints
                .Where(t => tpIds.Contains(t.Id))
                .Select(t => t.Name)
                .ToArray();

            cbTradePoint.DataSource = null;   // ВАЖНО

            cbTradePoint.Items.Clear();
            cbTradePoint.Items.AddRange(tradePoints);
        }

        private void LoadProductReportFilter()
        {
            cbReportFilter.DataSource = null;
            cbReportFilter.Items.Clear();

            cbReportFilter.Items.Add("Все торговые точки");
            cbReportFilter.Items.Add("По типу торговой точки");
            cbReportFilter.Items.Add("По конкретной торговой точке");

            cbReportFilter.SelectedIndex = 0;

            cbReportFilter.Visible = true;
            lblReportFilter.Visible = true;
        }

        private void ResetReportFilter()
        {
            cbReportFilter.SelectedIndexChanged -= cbReportFilter_SelectedIndexChanged;

            cbReportFilter.DataSource = null;
            cbReportFilter.Items.Clear();
            cbReportFilter.Text = "";

            cbReportFilter.Visible = false;
            lblReportFilter.Visible = false;

            cbReportFilter.SelectedIndexChanged += cbReportFilter_SelectedIndexChanged;
        }

        private void LoadReportFilter(string reportName)
        {
            cbReportFilter.DataSource = null;
            cbReportFilter.Items.Clear();

            cbReportFilter.Visible = true;
            lblReportFilter.Visible = true;

            switch (reportName)
            {
                // товарные отчёты
                case "Поставщики товара":
                case "Покупатели товара":
                    cbReportFilter.Items.Add("По товару");
                    cbReportFilter.Items.Add("По виду товара");
                    break;

                // точечные отчёты (единый стандарт)
                case "Цены товара по точкам":
                case "Объём продаж товара":
                case "Выработка продавцов":
                case "Зарплата продавцов":
                case "Товарооборот":
                    cbReportFilter.Items.Add("Все торговые точки");
                    cbReportFilter.Items.Add("По типу торговой точки");
                    cbReportFilter.Items.Add("По конкретной торговой точке");
                    break;

                default:
                    cbReportFilter.Visible = false;
                    lblReportFilter.Visible = false;
                    return;
            }

            cbReportFilter.SelectedIndex = 0;
        }

        private void LoadTradePointsForSale()
        {
            cbSaleTradePoint.DataSource = null;

            cbSaleTradePoint.DataSource = Program.Store.TradePoints;
            cbSaleTradePoint.DisplayMember = "Name";
            cbSaleTradePoint.ValueMember = "Id";

            if (cbSaleTradePoint.Items.Count > 0)
                cbSaleTradePoint.SelectedIndex = 0;
        }

        private void LoadSellersForSelectedTradePoint(int tradePointId)
        {
            cbSaleSeller.DataSource = null;

            cbSaleSeller.DataSource = Program.Store.Sellers
                .Where(s => s.TradePointId == tradePointId)
                .ToList();

            cbSaleSeller.DisplayMember = "Name";
            cbSaleSeller.ValueMember = "Id";

            if (cbSaleSeller.Items.Count > 0)
                cbSaleSeller.SelectedIndex = 0;
        }
    }
}