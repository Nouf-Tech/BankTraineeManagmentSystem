using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BankTraineeManagmentSystem
{
    public partial class Form1 : Form
    {
        private readonly string connString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BankManagmentDB;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password!", "Login Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "SELECT UserID, UserName, UserType FROM Users WHERE UserName = @u AND Password = @p";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = Convert.ToInt32(reader["UserID"]);
                            string name = reader["UserName"].ToString();
                            string type = reader["UserType"].ToString();

                            MessageBox.Show($"Welcome {type} {name}", "Login Successful",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            reader.Close();
                            conn.Close();

                            this.Hide();

                            if (type == "Employee")
                            {
                                EmployeeForm empForm = new EmployeeForm(userId, name);
                                empForm.ShowDialog();
                            }
                            else if (type == "Trainee")
                            {
                                TraineeForm traineeForm = new TraineeForm(userId, name);
                                traineeForm.ShowDialog();
                            }
                            else
                            {
                                MessageBox.Show("Unknown user type!", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                            this.Show();
                            ClearFields();
                        }
                        else
                        {
                            MessageBox.Show("Wrong username or password!", "Login Failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            txtUsername.Text = "";
            txtPassword.Text = "";
            txtUsername.Focus();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 1. خلفية الفورم الرئيسي (رمادي ناعم جداً مريح للعين)
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.BackgroundImage = null; // إزالة الشعار من الخلفية نهائياً

            // 2. البانيل (الكرت الأبيض في المنتصف)
            if (panel1 != null)
            {
                panel1.BackColor = Color.White;
                panel1.BorderStyle = BorderStyle.None;
            }

            // 3. زر LOGIN
            if (btnLogin != null)
            {
                btnLogin.BackColor = Color.FromArgb(0, 82, 136); // أزرق بنك التنمية
                btnLogin.ForeColor = Color.White;
                btnLogin.FlatStyle = FlatStyle.Flat;
                btnLogin.FlatAppearance.BorderSize = 0;
                btnLogin.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                btnLogin.Cursor = Cursors.Hand;
                SetRoundedRegion(btnLogin, 12); // تنعيم زوايا الزر
            }

            // 4. الـ TextBoxes
            ConfigureTextBox(txtUsername);
            ConfigureTextBox(txtPassword);

            CenterPanel();
            this.Resize += Form1_Resize;
        }

        private void ConfigureTextBox(TextBox textBox)
        {
            if (textBox != null)
            {
                textBox.BackColor = Color.WhiteSmoke;
                textBox.Font = new Font("Segoe UI", 11);
                textBox.BorderStyle = BorderStyle.FixedSingle;
                SetRoundedRegion(textBox, 6); // تنعيم الحقول
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            CenterPanel();
        }

        private void CenterPanel()
        {
            if (panel1 != null)
            {
                panel1.Left = (this.ClientSize.Width - panel1.Width) / 2;
                panel1.Top = (this.ClientSize.Height - panel1.Height) / 2;
            }
        }

        // رسم زوايا منحنية ناعمة جداً للبانيل (مثل تصميم الصورة)
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cornerRadius = 24;
            Rectangle bounds = new Rectangle(0, 0, panel1.Width, panel1.Height);

            using (GraphicsPath path = GetRoundedRectanglePath(bounds, cornerRadius))
            {
                panel1.Region = new Region(path);
                using (Pen pen = new Pen(Color.FromArgb(15, 0, 0, 0), 2))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void SetRoundedRegion(Control control, int radius)
        {
            Rectangle bounds = new Rectangle(0, 0, control.Width, control.Height);
            using (GraphicsPath path = GetRoundedRectanglePath(bounds, radius))
            {
                control.Region = new Region(path);
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void textBox2_TextChanged(object sender, EventArgs e) { }

        private void pictureBox1_Click(object sender, EventArgs e) { }
    }
}