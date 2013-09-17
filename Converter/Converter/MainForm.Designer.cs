namespace Converter
{
    partial class MainForm
    {
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.convert_button = new System.Windows.Forms.Button();
            this.results_label = new System.Windows.Forms.Label();
            this.treks_upload_button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(284, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // convert_button
            // 
            this.convert_button.Location = new System.Drawing.Point(26, 13);
            this.convert_button.Name = "convert_button";
            this.convert_button.Size = new System.Drawing.Size(227, 38);
            this.convert_button.TabIndex = 1;
            this.convert_button.Text = "Convert dates and prices to website format";
            this.convert_button.UseVisualStyleBackColor = true;
            // 
            // results_label
            // 
            this.results_label.AutoSize = true;
            this.results_label.Location = new System.Drawing.Point(23, 107);
            this.results_label.Name = "results_label";
            this.results_label.Size = new System.Drawing.Size(116, 13);
            this.results_label.TabIndex = 2;
            this.results_label.Text = "Click a button to begin!";
            this.results_label.Click += new System.EventHandler(this.results_label_Click);
            // 
            // treks_upload_button
            // 
            this.treks_upload_button.Location = new System.Drawing.Point(26, 58);
            this.treks_upload_button.Name = "treks_upload_button";
            this.treks_upload_button.Size = new System.Drawing.Size(227, 34);
            this.treks_upload_button.TabIndex = 3;
            this.treks_upload_button.Text = "Upload dates to thomsontreks.com (convert with the above button first)";
            this.treks_upload_button.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.treks_upload_button);
            this.Controls.Add(this.results_label);
            this.Controls.Add(this.convert_button);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "Converter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.Button convert_button;
        private System.Windows.Forms.Label results_label;
        private System.Windows.Forms.Button treks_upload_button;
    }
}

