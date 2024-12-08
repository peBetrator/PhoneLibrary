using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace Phone
{
    public class MainDialog : Form
    {
        private MobilePhone _currentPhone;

        public MainDialog()
        {
            // Установка параметров окна
            Text = "Mobile Phone Manager";
            Size = new System.Drawing.Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;

            // Инициализация пользовательских элементов
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Создаем кнопки и элементы управления
            var btnCreate = new Button { Text = "Create New", Location = new System.Drawing.Point(20, 20) };
            var btnSave = new Button { Text = "Save", Location = new System.Drawing.Point(20, 60) };
            var btnLoad = new Button { Text = "Load", Location = new System.Drawing.Point(20, 100) };
            var btnEdit = new Button { Text = "Edit", Location = new System.Drawing.Point(20, 140) };
            var btnDelete = new Button { Text = "Delete", Location = new System.Drawing.Point(20, 180) };
            var lblDisplay = new Label { Text = "No phone loaded.", Location = new System.Drawing.Point(150, 20), AutoSize = true };

            // Добавляем элементы в форму
            Controls.Add(btnCreate);
            Controls.Add(btnSave);
            Controls.Add(btnLoad);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(lblDisplay);

            // Обработчики событий
            btnCreate.Click += (s, e) => CreateNewPhone(lblDisplay);
            btnSave.Click += (s, e) => SavePhone(lblDisplay);
            btnLoad.Click += (s, e) => LoadPhone(lblDisplay);
            btnEdit.Click += (s, e) => EditPhone(lblDisplay);
            btnDelete.Click += (s, e) => DeletePhone(lblDisplay);
        }

        private TextBox CreateTextBoxWithLabel(Panel form, string labelText, ref int y, ToolTip toolTip, string toolTipText)
        {
            var label = new Label { Text = labelText, Location = new System.Drawing.Point(20, y), AutoSize = true };
            var textBox = new TextBox { Location = new System.Drawing.Point(150, y), Width = 200 };
            toolTip.SetToolTip(textBox, toolTipText);
            form.Controls.Add(label);
            form.Controls.Add(textBox);
            y += 40;
            return textBox;
        }

        private NumericUpDown CreateNumericUpDownWithLabel(Panel form, string labelText, ref int y, ToolTip toolTip, decimal min, decimal max, int decimalPlaces, string toolTipText)
        {
            var label = new Label { Text = labelText, Location = new System.Drawing.Point(20, y), AutoSize = true };
            var numericUpDown = new NumericUpDown
            {
                Location = new System.Drawing.Point(150, y),
                Width = 200,
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimalPlaces
            };
            toolTip.SetToolTip(numericUpDown, toolTipText);
            form.Controls.Add(label);
            form.Controls.Add(numericUpDown);
            y += 40;
            return numericUpDown;
        }

        private System.Windows.Forms.CheckBox CreateCheckBoxWithLabel(Panel form, string labelText, ref int y, ToolTip toolTip, string toolTipText)
        {
            var label = new Label { Text = labelText, Location = new System.Drawing.Point(20, y), AutoSize = true };
            var checkBox = new System.Windows.Forms.CheckBox { Location = new System.Drawing.Point(150, y) };
            toolTip.SetToolTip(checkBox, toolTipText);
            form.Controls.Add(label);
            form.Controls.Add(checkBox);
            y += 40;
            return checkBox;
        }

        private ComboBox CreateComboBoxWithLabel(Panel form, string labelText, ref int y, ToolTip toolTip, Type enumType)
        {
            var label = new Label { Text = labelText, Location = new System.Drawing.Point(20, y), AutoSize = true };
            var comboBox = new ComboBox
            {
                Location = new System.Drawing.Point(150, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(enumType)
            };
            toolTip.SetToolTip(comboBox, $"Select {labelText.ToLower()}.");
            form.Controls.Add(label);
            form.Controls.Add(comboBox);
            y += 40;
            return comboBox;
        }

        private Func<NetworkRange> CreateRadioButtonGroupWithLabel(Panel form, string labelText, ref int y, string[] options, string toolTipText)
        {
            var label = new Label { Text = labelText, Location = new System.Drawing.Point(20, y), AutoSize = true };
            var groupBox = new System.Windows.Forms.GroupBox
            {
                Location = new System.Drawing.Point(20, y + 20),
                Size = new System.Drawing.Size(350, 100),
                Text = labelText
            };

            System.Windows.Forms.RadioButton selectedButton = null;
            var radioButtons = new Dictionary<System.Windows.Forms.RadioButton, NetworkRange>();

            for (int i = 0; i < options.Length; i++)
            {
                var radioButton = new System.Windows.Forms.RadioButton
                {
                    Text = options[i],
                    Location = new System.Drawing.Point(10, 20 + (i * 20)),
                    AutoSize = true
                };

                var range = (NetworkRange)Enum.Parse(typeof(NetworkRange), options[i].Replace("2G", "TwoG")
                                                                                .Replace("3G", "ThreeG")
                                                                                .Replace("4G", "FourG")
                                                                                .Replace("5G", "FiveG"));
                radioButtons.Add(radioButton, range);

                radioButton.CheckedChanged += (s, e) =>
                {
                    if (radioButton.Checked)
                    {
                        selectedButton = radioButton;
                    }
                };

                groupBox.Controls.Add(radioButton);
                if (i == 0) radioButton.Checked = true; // Устанавливаем первый элемент по умолчанию
            }

            form.Controls.Add(label);
            form.Controls.Add(groupBox);
            y += 140;

            return () => radioButtons[selectedButton];
        }


        private void HandleFormClosing(FormClosingEventArgs e, bool isSaved)
        {
            if (!isSaved)
            {
                var result = MessageBox.Show("Changes are not saved. Do you want to discard them?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void CreateNewPhone(Label displayLabel)
        {
            var form = new Form
            {
                Text = "Create New Mobile Phone",
                Size = new System.Drawing.Size(420, 600), // Include space for VScrollBar
                StartPosition = FormStartPosition.CenterParent
            };

            var toolTip = new ToolTip();
            bool isSaved = false;

            // Panel to hold content
            var containerPanel = new Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(400, 600), // Width excludes VScrollBar
                AutoScroll = true
            };

            form.Controls.Add(containerPanel);

            int y = 20; // Starting Y-coordinate for controls

            // Add controls for input
            var brandTextBox = CreateTextBoxWithLabel(containerPanel, "Brand:", ref y, toolTip, "Enter the brand of the phone.");
            var modelTextBox = CreateTextBoxWithLabel(containerPanel, "Model:", ref y, toolTip, "Enter the model of the phone.");
            var priceNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Price:", ref y, toolTip, 0, 10000, 2, "Enter the price of the phone.");
            var dualSimCheckBox = CreateCheckBoxWithLabel(containerPanel, "Dual SIM:", ref y, toolTip, "Check if the phone supports dual SIM cards.");
            var osComboBox = CreateComboBoxWithLabel(containerPanel, "Operating System:", ref y, toolTip, typeof(OperatingSystemType));
            var displayComboBox = CreateComboBoxWithLabel(containerPanel, "Display Type:", ref y, toolTip, typeof(DisplayType));
            var batteryNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Battery (mAh):", ref y, toolTip, 1000, 10000, 0, "Enter the battery capacity in mAh.");
            var releaseYearNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Release Year:", ref y, toolTip, 2000, DateTime.Now.Year, 0, "Enter the release year of the phone.");
            var is5GCheckBox = CreateCheckBoxWithLabel(containerPanel, "5G Capable:", ref y, toolTip, "Check if the phone supports 5G.");
            var memoryNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Memory (GB):", ref y, toolTip, 1, 1024, 0, "Enter the memory size in GB.");

            // Add radio buttons for network range
            var selectedRange = CreateRadioButtonGroupWithLabel(containerPanel, "Network Range:", ref y, new[] { "2G", "3G", "4G", "5G" }, "Select the network range of the phone.");

            // Add save button
            var saveButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(150, y),
                Width = 100
            };
            saveButton.Click += (s, e) =>
            {
                _currentPhone = new MobilePhone(
                    brandTextBox.Text,
                    modelTextBox.Text,
                    priceNumericUpDown.Value,
                    dualSimCheckBox.Checked,
                    (OperatingSystemType)osComboBox.SelectedItem,
                    (DisplayType)displayComboBox.SelectedItem,
                    (int)batteryNumericUpDown.Value,
                    new DateTime((int)releaseYearNumericUpDown.Value, 1, 1),
                    is5GCheckBox.Checked,
                    (int)memoryNumericUpDown.Value,
                    selectedRange()
                );

                isSaved = true;
                MessageBox.Show("Phone created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Close();
                displayLabel.Text = "Phone created:\n" + PhoneToString(_currentPhone);
            };

            containerPanel.Controls.Add(saveButton);

            // Handle form closing
            form.FormClosing += (s, e) => HandleFormClosing(e, isSaved);

            form.ShowDialog();
        }




        private void SavePhone(Label displayLabel)
        {
            if (_currentPhone == null)
            {
                MessageBox.Show("No phone to save!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var saveDialog = new SaveFileDialog { Filter = "XML Files|*.xml" };
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var serializer = new XmlSerializer(typeof(MobilePhone));
                using (var stream = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    serializer.Serialize(stream, _currentPhone);
                }
                displayLabel.Text = "Phone saved to file.";
            }
        }

        private void LoadPhone(Label displayLabel)
        {
            var openDialog = new OpenFileDialog { Filter = "XML Files|*.xml" };
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                var serializer = new XmlSerializer(typeof(MobilePhone));
                using (var stream = new FileStream(openDialog.FileName, FileMode.Open))
                {
                    _currentPhone = (MobilePhone)serializer.Deserialize(stream);
                }
                displayLabel.Text = "Phone loaded:\n" + PhoneToString(_currentPhone);
            }
        }

        private void EditPhone(Label displayLabel)
        {
            if (_currentPhone == null)
            {
                MessageBox.Show("No phone to edit!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var form = new Form
            {
                Text = "Edit Mobile Phone",
                Size = new System.Drawing.Size(420, 600), // Дополнительное место для VScrollBar
                StartPosition = FormStartPosition.CenterParent
            };

            // Panel to hold content
            var containerPanel = new Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(400, 600), // Width excludes VScrollBar
                AutoScroll = true
            };

            form.Controls.Add(containerPanel);


            var toolTip = new ToolTip();
            bool isSaved = false;
            int y = 20;

            // Поля редактирования
            var brandTextBox = CreateTextBoxWithLabel(containerPanel, "Brand:", ref y, toolTip, "Enter the brand of the phone.");
            brandTextBox.Text = _currentPhone.Brand;

            var modelTextBox = CreateTextBoxWithLabel(containerPanel, "Model:", ref y, toolTip, "Enter the model of the phone.");
            modelTextBox.Text = _currentPhone.Model;

            var priceNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Price:", ref y, toolTip, 0, 10000, 2, "Enter the price of the phone.");
            priceNumericUpDown.Value = _currentPhone.Price;

            var dualSimCheckBox = CreateCheckBoxWithLabel(containerPanel, "Dual SIM:", ref y, toolTip, "Check if the phone supports dual SIM cards.");
            dualSimCheckBox.Checked = _currentPhone.IsDualSim;

            var osComboBox = CreateComboBoxWithLabel(containerPanel, "Operating System:", ref y, toolTip, typeof(OperatingSystemType));
            osComboBox.SelectedItem = _currentPhone.OperatingSystem;

            var displayComboBox = CreateComboBoxWithLabel(containerPanel, "Display Type:", ref y, toolTip, typeof(DisplayType));
            displayComboBox.SelectedItem = _currentPhone.Display;

            var batteryNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Battery (mAh):", ref y, toolTip, 1000, 10000, 0, "Enter the battery capacity in mAh.");
            batteryNumericUpDown.Value = _currentPhone.BatteryLevel;

            var releaseYearNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Release Year:", ref y, toolTip, 2000, DateTime.Now.Year, 0, "Enter the release year of the phone.");
            releaseYearNumericUpDown.Value = _currentPhone.ReleaseYear.Year;

            var is5GCheckBox = CreateCheckBoxWithLabel(containerPanel, "5G Capable:", ref y, toolTip, "Check if the phone supports 5G.");
            is5GCheckBox.Checked = _currentPhone.Is5GCapable;

            var memoryNumericUpDown = CreateNumericUpDownWithLabel(containerPanel, "Memory (GB):", ref y, toolTip, 1, 1024, 0, "Enter the memory size in GB.");
            memoryNumericUpDown.Value = _currentPhone.Memory;

            // Радио-кнопки для диапазона сети
            var selectedRange = CreateRadioButtonGroupWithLabel(containerPanel, "Network Range:", ref y, new[] { "2G", "3G", "4G", "5G" }, "Select the network range of the phone.");
            switch (_currentPhone.NetworkRange)
            {
                case NetworkRange.TwoG: selectedRange = () => NetworkRange.TwoG; break;
                case NetworkRange.ThreeG: selectedRange = () => NetworkRange.ThreeG; break;
                case NetworkRange.FourG: selectedRange = () => NetworkRange.FourG; break;
                case NetworkRange.FiveG: selectedRange = () => NetworkRange.FiveG; break;
            }

            // Кнопка сохранения
            var saveButton = new Button
            {
                Text = "Save Changes",
                Location = new System.Drawing.Point(150, y),
                Width = 100
            };
            saveButton.Click += (s, e) =>
            {
                _currentPhone.Brand = brandTextBox.Text;
                _currentPhone.Model = modelTextBox.Text;
                _currentPhone.Price = priceNumericUpDown.Value;
                _currentPhone.IsDualSim = dualSimCheckBox.Checked;
                _currentPhone.OperatingSystem = (OperatingSystemType)osComboBox.SelectedItem;
                _currentPhone.Display = (DisplayType)displayComboBox.SelectedItem;
                _currentPhone.BatteryLevel = (int)batteryNumericUpDown.Value;
                _currentPhone.ReleaseYear = new DateTime((int)releaseYearNumericUpDown.Value, 1, 1);
                _currentPhone.Is5GCapable = is5GCheckBox.Checked;
                _currentPhone.Memory = (int)memoryNumericUpDown.Value;
                _currentPhone.NetworkRange = selectedRange();

                isSaved = true;
                MessageBox.Show("Phone updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Close();
                displayLabel.Text = "Phone updated:\n" + PhoneToString(_currentPhone);
            };

            containerPanel.Controls.Add(saveButton);

            // Handle form closing
            form.FormClosing += (s, e) => HandleFormClosing(e, isSaved);

            form.ShowDialog();
        }

        private void DeletePhone(Label displayLabel)
        {
            if (_currentPhone == null)
            {
                MessageBox.Show("No phone to delete!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentPhone = null;
            displayLabel.Text = "Phone deleted.";
        }

        private string PhoneToString(MobilePhone phone)
        {
            return $"Brand: {phone.Brand}, Model: {phone.Model}, Price: {phone.Price:C}, " +
                   $"Dual SIM: {phone.IsDualSim}, OS: {phone.OperatingSystem}, Display: {phone.Display}, " +
                   $"Battery: {phone.BatteryLevel}mAh, Release: {phone.ReleaseYear.Year}, " +
                   $"5G: {phone.Is5GCapable}, Memory: {phone.Memory}GB";
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainDialog());
        }
    }

    public enum OperatingSystemType
    {
        Android,
        iOS,
        WindowsPhone,
        Other
    }

    public enum DisplayType
    {
        LCD,
        OLED,
        AMOLED,
        Retina,
        TFT,
        Other
    }

    public enum NetworkRange
    {
        TwoG,
        ThreeG,
        FourG,
        FiveG
    }


    public class MobilePhone
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public bool IsDualSim { get; set; }
        public OperatingSystemType OperatingSystem { get; set; }
        public DisplayType Display { get; set; }
        public int BatteryLevel { get; set; }
        public DateTime ReleaseYear { get; set; }
        public bool Is5GCapable { get; set; }
        public int Memory { get; set; }
        public NetworkRange NetworkRange { get; set; }

        public MobilePhone() { }

        public MobilePhone(string brand, string model, decimal price, bool isDualSim, OperatingSystemType operatingSystem,
            DisplayType display, int batteryLevel, DateTime releaseYear, bool is5GCapable, int memory, NetworkRange networkRange)
        {
            Brand = brand;
            Model = model;
            Price = price;
            IsDualSim = isDualSim;
            OperatingSystem = operatingSystem;
            Display = display;
            BatteryLevel = batteryLevel;
            ReleaseYear = releaseYear;
            Is5GCapable = is5GCapable;
            Memory = memory;
            NetworkRange = networkRange;
        }
    }
}
