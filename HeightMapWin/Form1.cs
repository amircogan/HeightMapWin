using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HeightMap4;

namespace HeightMapWin
{


    public partial class Form1 : Form
    {
        Point _aPoint = new Point(0,0);
        public Form1()
        {
            InitializeComponent();
            k200.Hide();
            add.Hide();
            pallete.Hide();
            comboBox1.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aPoint.X==0)
                _aPoint = blackwhite.Location;
            int idx = comboBox1.SelectedIndex;
            switch (idx)
            {
                case 0:
                    k200.Hide();
                    add.Hide();
                    pallete.Hide();
                    blackwhite.Show();
                    blackwhite.Location = new Point(_aPoint.X, _aPoint.Y);
                    one_color_modify.Hide();
                    break;
                case 1:
                    k200.Hide();
                    add.Hide();
                    blackwhite.Hide();
                    pallete.Show();
                    pallete.Location = new Point(_aPoint.X, _aPoint.Y);
                    one_color_modify.Hide();
                    break;
                case 2:
                    k200.Hide();
                    add.Show();
                    add.Location = new Point(_aPoint.X, _aPoint.Y);
                    pallete.Hide();
                    blackwhite.Hide();
                    one_color_modify.Hide();
                    break;
                case 3:
                    k200.Show();
                    k200.Location = new Point(_aPoint.X, _aPoint.Y);
                    add.Hide();
                    blackwhite.Hide();
                    pallete.Hide();
                    one_color_modify.Hide();
                    break;
                case 4:
                    k200.Hide();
                    add.Hide();
                    blackwhite.Hide();
                    pallete.Hide();
                    one_color_modify.Show();
                    one_color_modify.Location = new Point(_aPoint.X, _aPoint.Y); break;
            }
        }




        private string getFile()
        {
            string fileName = null;

            using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
            {
                //openFileDialog1.InitialDirectory = "c:\\";
                //openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                //openFileDialog1.FilterIndex = 2;
                //openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog1.FileName;
                }
            }
            return fileName;
        }


        // black and white image file
        private void button2_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox1.Text = fname;
        }


        // pallete image file
        private void button3_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox4.Text = fname;
        }


        // pallete pallete file
        private void button4_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox3.Text = fname;
        }


        // ants image file
        private void button8_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox10.Text = fname;
        }


        // add image file
        private void button6_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox6.Text = fname;
        }

        // add brush file
        private void button5_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox5.Text = fname;
        }

        // GO button
        private void button1_Click(object sender, EventArgs e)
        {
            string[] args = new string[6];
            switch (comboBox1.SelectedIndex)
            {
                case 0: // blackWhite
                    args[0] = "bw";
                    args[1] = textBox1.Text; // image file
                    args[2] = args[3] = args[4] = textBox2.Text;
                    break;

                case 1: // pallete file
                    args[0] = "pl1";
                    args[1] = textBox3.Text; //pallete file
                    args[2] = textBox4.Text;  // input image file

                    break;

                case 2: // add 2 files
                    args[0] = "add";
                    args[1] = textBox6.Text; // file #1
                    args[2] = textBox5.Text;  // file  #2
                    args[3] = textBox7.Text; // ratio #1/#2
                    break;

                case 3: // ant killer
                    args[0] = "k200";
                    args[1] = textBox10.Text; //image file
                    args[2] = textBox9.Text; //image file
                    args[3] = textBox8.Text; //image file
                    break;

                case 4: // one color modify
                    args[0] = "one_color_modify";
                    args[1] = textBox11.Text; // COLOR - say 255,255,255 is white
                    args[2] = textBox12.Text;  // color sensatvity, say 10 is plus minus 10 difference for each base color (cube, not radius for faster calc)
                    args[3] = textBox14.Text; // image file
                    args[4] = textBox13.Text;  // height file
                    break;
            }

            HeightMap4Main.setFormStuff(this, this.resultText);
            resultText.Text = "";
            HeightMap4Main.Start(args);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox14.Text = fname;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string fname = getFile();
            if (fname != null)
                textBox13.Text = fname;
        }
    }
}
