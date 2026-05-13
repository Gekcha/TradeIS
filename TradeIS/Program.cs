using System;
using System.Windows.Forms;

namespace TradeIS
{
    static class Program
    {
        // Глобальный доступ к данным через Program.Store
        public static DataStorage Store { get; set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Пытаемся загрузить данные из файла через StorageManager
            try
            {
                Store = StorageManager.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }

            // 2. Если файл пуст или отсутствует, создаем новый объект
            if (Store == null)
            {
                Store = new DataStorage();
            }
            else
            {
                // 3. Если данные загружены, ПРИНУДИТЕЛЬНО обновляем счетчики,
                // чтобы новые ID не конфликтовали со старыми
                Store.ActualizeCounters();
            }

            // 4. Запуск главной формы
            Application.Run(new MainForm());
        }
    }
}