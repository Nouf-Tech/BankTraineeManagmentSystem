using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Text;

namespace BankTraineeManagmentSystem
{
    public partial class TraineeForm : Form
    {
        int userId;
        string userName;
        string connString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BankManagmentDB;Integrated Security=True";

        public TraineeForm(int id, string name)
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            userId = id;
            userName = name;
            lblWelcome.Text = "Welcome, " + name;
            LoadMyTask();
            CustomizeDataGridView();
        }

        void LoadMyTask()
        {
            try
            {
                string query = @"
            SELECT 
                t.TaskID, 
                t.Title, 
                c.Name AS ClientName, 
                t.Status,
                t.Priority
            FROM Tasks t
            INNER JOIN Clients c ON t.ClientLoanNumber = c.LoanNumber
            WHERE t.AssignedTo = @userId
            AND t.Status != 'Completed'  -- ← إخفاء المكتملة
            ORDER BY 
                CASE t.Priority
                    WHEN 'High' THEN 1
                    WHEN 'Normal' THEN 2
                    WHEN 'Low' THEN 3
                    ELSE 4
                END,
                t.TaskID DESC";

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }

                dgvMyTask.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading tasks: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        void CustomizeDataGridView()
        {
            dgvMyTask.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMyTask.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMyTask.MultiSelect = false;
            dgvMyTask.ReadOnly = true;

            dgvMyTask.EnableHeadersVisualStyles = false;
            dgvMyTask.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.SteelBlue;
            dgvMyTask.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            dgvMyTask.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            dgvMyTask.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
            dgvMyTask.RowTemplate.Height = 35;
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvMyTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task!");
                return;
            }

            int taskId = Convert.ToInt32(dgvMyTask.SelectedRows[0].Cells["TaskID"].Value);
            string currentStatus = dgvMyTask.SelectedRows[0].Cells["Status"].Value.ToString();
            string taskTitle = dgvMyTask.SelectedRows[0].Cells["Title"].Value.ToString();

            string status = Interaction.InputBox(
                $"Task: {taskTitle}\n" +
                $"Current Status: {currentStatus}\n\n" +
                $"Select new status:\n" +
                $"[1] New\n" +
                $"[2] InProgress\n" +
                $"[3] Completed\n\n" +
                $"Enter number or status name:",
                "Update Status",
                currentStatus);

            if (string.IsNullOrEmpty(status)) return;

            status = status.Trim();
            switch (status)
            {
                case "1": status = "New"; break;
                case "2": status = "InProgress"; break;
                case "3": status = "Completed"; break;
            }

            string[] validStatuses = { "New", "InProgress", "Completed" };
            bool isValid = false;

            foreach (string validStatus in validStatuses)
            {
                if (status.Equals(validStatus, StringComparison.OrdinalIgnoreCase))
                {
                    status = validStatus;
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                MessageBox.Show("Invalid status! Please use:\n• New\n• InProgress\n• Completed");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = "UPDATE Tasks SET Status = @status WHERE TaskID = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"Status updated to: {status}!");
                LoadMyTask();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnAddNote_Click(object sender, EventArgs e)
        {
            if (dgvMyTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task first.", "No Task Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (dgvMyTask.SelectedRows[0].Cells["TaskID"].Value == null)
                {
                    MessageBox.Show("Invalid task selected.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int taskId = Convert.ToInt32(dgvMyTask.SelectedRows[0].Cells["TaskID"].Value);
                string taskTitle = dgvMyTask.SelectedRows[0].Cells["Title"].Value.ToString();
                string clientName = dgvMyTask.SelectedRows[0].Cells["ClientName"].Value.ToString();

                string noteText = Interaction.InputBox(
                    $"Task: {taskTitle}\n" +
                    $"Client: {clientName}\n\n" +
                    $"Enter your note:",
                    "Add Note");

                if (string.IsNullOrWhiteSpace(noteText))
                {
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string getLoanQuery = "SELECT ClientLoanNumber FROM Tasks WHERE TaskID = @taskId";
                    using (SqlCommand getCmd = new SqlCommand(getLoanQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@taskId", taskId);
                        object result = getCmd.ExecuteScalar();

                        if (result == null)
                        {
                            MessageBox.Show("Task not found in database.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        long loanNumber = Convert.ToInt64(result);

                        string query = "INSERT INTO Notes (UserID, ClientLoanNumber, NoteText) VALUES (@user, @loan, @note)";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@user", userId);
                            cmd.Parameters.AddWithValue("@loan", loanNumber);
                            cmd.Parameters.AddWithValue("@note", noteText);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Note added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding note: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadMyTask();
            MessageBox.Show("Tasks refreshed!");
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvMyTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task!");
                return;
            }

            int taskId = Convert.ToInt32(dgvMyTask.SelectedRows[0].Cells["TaskID"].Value);
            string taskTitle = dgvMyTask.SelectedRows[0].Cells["Title"].Value.ToString();
            string clientName = dgvMyTask.SelectedRows[0].Cells["ClientName"].Value.ToString();
            string status = dgvMyTask.SelectedRows[0].Cells["Status"].Value.ToString();
            string priority = dgvMyTask.SelectedRows[0].Cells["Priority"].Value.ToString();

            string details = GetTaskDetails(taskId);

            MessageBox.Show(
                $"Task Details:\n\n" +
                $"Title: {taskTitle}\n" +
                $"Client: {clientName}\n" +
                $"Status: {status}\n" +
                $"Priority: {priority}\n\n" +
                details,
                "Task Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private string GetTaskDetails(int taskId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            t.TaskDescription,
                            c.LoanNumber,
                            c.Phone,
                            c.LoanAmount,
                            u.UserName AS AssignedBy
                        FROM Tasks t
                        INNER JOIN Clients c ON t.ClientLoanNumber = c.LoanNumber
                        LEFT JOIN Users u ON t.EmployeeID = u.UserID
                        WHERE t.TaskID = @taskId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        StringBuilder details = new StringBuilder();

                        if (!reader.IsDBNull(0))
                            details.AppendLine($"Description: {reader.GetString(0)}");

                        details.AppendLine($"Loan Number: {reader.GetInt64(1)}");

                        if (!reader.IsDBNull(2))
                            details.AppendLine($"Client Phone: {reader.GetString(2)}");

                        if (!reader.IsDBNull(3))
                            details.AppendLine($"Loan Amount: {reader.GetDecimal(3):N2} SAR");

                        if (!reader.IsDBNull(4))
                            details.AppendLine($"Assigned by: {reader.GetString(4)}");

                        reader.Close();
                        return details.ToString();
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                return "Error loading details: " + ex.Message;
            }

            return "";
        }

        private void dgvMyTask_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgvMyTask_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                btnEdit_Click(sender, e);
            }
        }

        private void TraineeForm_Load(object sender, EventArgs e)
        {

        }

        private void lblWelcome_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string query = @"
            SELECT 
                t.TaskID, 
                t.Title, 
                c.Name AS ClientName, 
                t.Status,
                t.Priority
            FROM Tasks t
            INNER JOIN Clients c ON t.ClientLoanNumber = c.LoanNumber
            WHERE t.AssignedTo = @userId
            AND t.Status = 'Completed'  -- ← عرض المكتملة فقط
            ORDER BY t.TaskID DESC";

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("No completed tasks yet!");
                    return;
                }

                dgvMyTask.DataSource = dt;
                MessageBox.Show($"Showing {dt.Rows.Count} completed tasks.\nClick Refresh to go back to active tasks.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}