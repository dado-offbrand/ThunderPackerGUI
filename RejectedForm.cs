using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThunderPacker
{
    public partial class RejectedForm : Form
    {
        public RejectedForm()
        {
            InitializeComponent();
        }

        public void SetError(string err) 
        {
            l_Error.Text = err;
        }

        private void b_Close_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
