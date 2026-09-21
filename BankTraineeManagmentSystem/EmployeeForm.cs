using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;

namespace BankTraineeManagmentSystem
{
    public partial class EmployeeForm : Form
    {
        int userId;
        string userName;
        string connString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BankManagmentDB;Integrated Security=True";

        public EmployeeForm(int id, string name)
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            userId = id;
            userName = name;
            lblWelcome.Text = "Welcome, " + name;
            LoadClients();
            CustomizeDataGridView();
        }

        void LoadClients()
        {
            try
            {
                string query = "SELECT LoanNumber, Name, Phone, LoanAmount FROM Clients";
                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    adapter.Fill(dt);
                }

                dgvClients.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading clients: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void CustomizeDataGridView()
        {
            dgvClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvClients.MultiSelect = false;
            dgvClients.ReadOnly = true;

            dgvClients.EnableHeadersVisualStyles = false;
            dgvClients.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.SteelBlue;
            dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            dgvClients.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            dgvClients.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
        }

        private void btnAddClients_Click(object sender, EventArgs e)
        {
            string loanNumber = Interaction.InputBox("Loan Number:", "Add Client");
            if (string.IsNullOrEmpty(loanNumber)) return;

            string clientName = Interaction.InputBox("Client Name:", "Add Client");
            if (string.IsNullOrEmpty(clientName)) return;

            string phone = Interaction.InputBox("Phone:", "Add Client");
            if (string.IsNullOrEmpty(phone)) return;

            string loanAmount = Interaction.InputBox("Loan Amount:", "Add Client");
            if (string.IsNullOrEmpty(loanAmount)) return;

            long loanNum;
            decimal amount;

            if (!long.TryParse(loanNumber, out loanNum))
            {
                MessageBox.Show("Loan number must be a valid number!", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(loanAmount, out amount) || amount <= 0)
            {
                MessageBox.Show("Loan amount must be a valid positive number!", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM Clients WHERE LoanNumber = @loan";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@loan", loanNum);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("This loan number already exists!", "Duplicate Entry",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string query = "INSERT INTO Clients (LoanNumber, Name, Phone, LoanAmount, LoanTypeID) VALUES (@loan, @name, @phone, @amount, 1)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@loan", loanNum);
                    cmd.Parameters.AddWithValue("@name", clientName);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Client added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding client: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteClients_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count > 0)
            {
                long loanNumber = Convert.ToInt64(dgvClients.SelectedRows[0].Cells["LoanNumber"].Value);
                string clientName = dgvClients.SelectedRows[0].Cells["Name"].Value.ToString();

                var result = MessageBox.Show(
                    $"Are you sure you want to delete client '{clientName}' with loan number {loanNumber}?\n\nThis will also delete all related tasks and notes!",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connString))
                        {
                            conn.Open();

                            string deleteTasksQuery = "DELETE FROM Tasks WHERE ClientLoanNumber = @loan";
                            SqlCommand deleteTasksCmd = new SqlCommand(deleteTasksQuery, conn);
                            deleteTasksCmd.Parameters.AddWithValue("@loan", loanNumber);
                            deleteTasksCmd.ExecuteNonQuery();

                            string deleteNotesQuery = "DELETE FROM Notes WHERE ClientLoanNumber = @loan";
                            SqlCommand deleteNotesCmd = new SqlCommand(deleteNotesQuery, conn);
                            deleteNotesCmd.Parameters.AddWithValue("@loan", loanNumber);
                            deleteNotesCmd.ExecuteNonQuery();

                            string query = "DELETE FROM Clients WHERE LoanNumber = @loan";
                            SqlCommand cmd = new SqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@loan", loanNumber);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Client deleted successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadClients();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting client: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a client to delete!", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnAssignTask_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a client first!");
                return;
            }

            long loanNumber = Convert.ToInt64(dgvClients.SelectedRows[0].Cells["LoanNumber"].Value);
            string clientName = dgvClients.SelectedRows[0].Cells["Name"].Value.ToString();

            string availableTrainees = GetAvailableTrainees();
            if (string.IsNullOrEmpty(availableTrainees))
            {
                MessageBox.Show("No trainees available!");
                return;
            }

            string traineeId = Interaction.InputBox(
                "Available Trainees:\n\n" + availableTrainees + "\nEnter Trainee ID:",
                "Select Trainee");

            if (string.IsNullOrEmpty(traineeId)) return;

            if (!int.TryParse(traineeId, out int traineeIdNum))
            {
                MessageBox.Show("Invalid ID!");
                return;
            }

            string traineeName = GetTraineeName(traineeIdNum);
            if (string.IsNullOrEmpty(traineeName))
            {
                MessageBox.Show("Trainee not found!");
                return;
            }

            string taskTitle = Interaction.InputBox($"Task Title:\n\nClient: {clientName}\nTrainee: {traineeName}", "Task Title");
            if (string.IsNullOrEmpty(taskTitle))
            {
                MessageBox.Show("Task title is required!");
                return;
            }

            string taskDesc = Interaction.InputBox("Task Description (Optional):", "Description");

            string priority = Interaction.InputBox("Priority (High / Normal / Low):", "Priority", "Normal");
            if (string.IsNullOrEmpty(priority)) priority = "Normal";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO Tasks 
                        (Title, EmployeeID, TaskDescription, AssignedTo, ClientLoanNumber, Priority, Status)
                        VALUES 
                        (@title, @emp, @desc, @trainee, @loan, @priority, 'Pending')";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@title", taskTitle);
                    cmd.Parameters.AddWithValue("@emp", this.userId);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(taskDesc) ? (object)DBNull.Value : taskDesc);
                    cmd.Parameters.AddWithValue("@trainee", traineeIdNum);
                    cmd.Parameters.AddWithValue("@loan", loanNumber);
                    cmd.Parameters.AddWithValue("@priority", priority);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"Task assigned successfully to {traineeName}!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private string GetAvailableTrainees()
        {
            StringBuilder trainees = new StringBuilder();

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT UserID, UserName FROM Users WHERE UserType = 'Trainee' ORDER BY UserID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        trainees.AppendLine($"{id} - {name}");
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading trainees: " + ex.Message);
            }

            return trainees.ToString();
        }

        private string GetTraineeName(int traineeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT UserName FROM Users WHERE UserID = @id AND UserType = 'Trainee'";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", traineeId);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadClients();
            MessageBox.Show("Clients list refreshed!", "Refresh",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a client first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                long loanNumber = Convert.ToInt64(dgvClients.SelectedRows[0].Cells["LoanNumber"].Value);
                string clientName = dgvClients.SelectedRows[0].Cells["Name"].Value.ToString();

                string query = @"
            SELECT 
                u.UserName,
                n.NoteText
            FROM Notes n
            INNER JOIN Users u ON n.UserID = u.UserID
            WHERE n.ClientLoanNumber = @loan
            ORDER BY n.NoteID DESC";

                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@loan", loanNumber);

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show($"No notes found for client: {clientName}", "No Notes",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Client: {clientName}");
                sb.AppendLine($"Loan Number: {loanNumber}");
                sb.AppendLine($"Total Notes: {dt.Rows.Count}\n");

                int noteNumber = 1;
                foreach (DataRow row in dt.Rows)
                {
                    sb.AppendLine($"Note #{noteNumber}");
                    sb.AppendLine($"By: {row["UserName"]}");
                    sb.AppendLine($"Note: {row["NoteText"]}");
                    sb.AppendLine();
                    noteNumber++;
                }

                MessageBox.Show(sb.ToString(), "Client Notes",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ViewTasks_Click(object sender, EventArgs e)
        {
            if (dgvClients.SelectedRows.Count > 0)
            {
                long loanNumber = Convert.ToInt64(dgvClients.SelectedRows[0].Cells["LoanNumber"].Value);
                ShowTasksForClient(loanNumber);
            }
            else
            {
                MessageBox.Show("Please select a client first!", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowTasksForClient(long loanNumber)
        {
            try
            {
                string query = @"
                    SELECT 
                        t.TaskID, 
                        t.Title, 
                        t.Status, 
                        t.Priority,
                        u.UserName AS TraineeName
                    FROM Tasks t
                    INNER JOIN Users u ON t.AssignedTo = u.UserID
                    WHERE t.ClientLoanNumber = @loan";

                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@loan", loanNumber);

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    string tasks = $"Tasks for Client (Loan #{loanNumber}):\n\n";
                    foreach (DataRow row in dt.Rows)
                    {
                        tasks += $"• {row["Title"]} - {row["Status"]} ({row["Priority"]}) - Assigned to: {row["TraineeName"]}\n";
                    }
                    MessageBox.Show(tasks, "Client Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No tasks found for this client!", "No Tasks",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading tasks: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvClients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}