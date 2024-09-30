namespace TransactionSummary_Winform
{
    partial class Form1
    {
        private ComboBox comboBoxStartYear;
        private ComboBox comboBoxEndYear;
        private ComboBox comboBoxStartMonth;
        private ComboBox comboBoxEndMonth;
        private TextBox textBoxTransactionTypes;
        private Button buttonProcess;
        private DataGridView dataGridViewResults;
        private Label labelStatus;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBoxStartYear = new ComboBox();
            comboBoxEndYear = new ComboBox();
            comboBoxStartMonth = new ComboBox();
            comboBoxEndMonth = new ComboBox();
            textBoxTransactionTypes = new TextBox();
            buttonProcess = new Button();
            dataGridViewResults = new DataGridView();
            labelStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            SuspendLayout();
            // 
            // comboBoxStartYear
            // 
            comboBoxStartYear.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxStartYear.Location = new Point(23, 23);
            comboBoxStartYear.Margin = new Padding(4, 3, 4, 3);
            comboBoxStartYear.Name = "comboBoxStartYear";
            comboBoxStartYear.Size = new Size(116, 23);
            comboBoxStartYear.TabIndex = 0;
            // 
            // comboBoxEndYear
            // 
            comboBoxEndYear.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxEndYear.Location = new Point(163, 23);
            comboBoxEndYear.Margin = new Padding(4, 3, 4, 3);
            comboBoxEndYear.Name = "comboBoxEndYear";
            comboBoxEndYear.Size = new Size(116, 23);
            comboBoxEndYear.TabIndex = 1;
            // 
            // comboBoxStartMonth
            // 
            comboBoxStartMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxStartMonth.Location = new Point(23, 69);
            comboBoxStartMonth.Margin = new Padding(4, 3, 4, 3);
            comboBoxStartMonth.Name = "comboBoxStartMonth";
            comboBoxStartMonth.Size = new Size(116, 23);
            comboBoxStartMonth.TabIndex = 2;
            // 
            // comboBoxEndMonth
            // 
            comboBoxEndMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxEndMonth.Location = new Point(163, 69);
            comboBoxEndMonth.Margin = new Padding(4, 3, 4, 3);
            comboBoxEndMonth.Name = "comboBoxEndMonth";
            comboBoxEndMonth.Size = new Size(116, 23);
            comboBoxEndMonth.TabIndex = 3;
            // 
            // textBoxTransactionTypes
            // 
            textBoxTransactionTypes.Location = new Point(23, 115);
            textBoxTransactionTypes.Margin = new Padding(4, 3, 4, 3);
            textBoxTransactionTypes.Name = "textBoxTransactionTypes";
            textBoxTransactionTypes.Size = new Size(256, 23);
            textBoxTransactionTypes.TabIndex = 4;
            textBoxTransactionTypes.Text = "deposit,withdraw,receipt,barcode";
            // 
            // buttonProcess
            // 
            buttonProcess.Location = new Point(23, 162);
            buttonProcess.Margin = new Padding(4, 3, 4, 3);
            buttonProcess.Name = "buttonProcess";
            buttonProcess.Size = new Size(257, 27);
            buttonProcess.TabIndex = 5;
            buttonProcess.Text = "Process";
            buttonProcess.UseVisualStyleBackColor = true;
            buttonProcess.Click += buttonProcess_Click;
            // 
            // dataGridViewResults
            // 
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Location = new Point(23, 208);
            dataGridViewResults.Margin = new Padding(4, 3, 4, 3);
            dataGridViewResults.Name = "dataGridViewResults";
            dataGridViewResults.Size = new Size(700, 231);
            dataGridViewResults.TabIndex = 6;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(23, 462);
            labelStatus.Margin = new Padding(4, 0, 4, 0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(0, 15);
            labelStatus.TabIndex = 7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(933, 519);
            Controls.Add(labelStatus);
            Controls.Add(dataGridViewResults);
            Controls.Add(buttonProcess);
            Controls.Add(textBoxTransactionTypes);
            Controls.Add(comboBoxEndMonth);
            Controls.Add(comboBoxStartMonth);
            Controls.Add(comboBoxEndYear);
            Controls.Add(comboBoxStartYear);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Transaction Summary Processor";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        

        #endregion
    }
}
