using System;
using System.Drawing;
using System.Windows.Forms;
using EasyModbus;
using MySql.Data.MySqlClient;

namespace DMAenergia
{
    public partial class SCADA : Form
    {
       ModbusClient Equipo1 = new ModbusClient();
       ModbusClient Equipo2 = new ModbusClient();
        public static int led = 0;
        int[]EntradaAnalogica;
        int[] EntradaAnalogicaPLC;
        int[] EntradaDigital;
        int[] aux = new int[50];

        public SCADA(){
            InitializeComponent();
            Equipo1.IPAddress = "192.168.10.10";//gensys core
            Equipo1.Port = 503;
            Equipo2.IPAddress = "192.168.10.132";
            Equipo2.Port = 502;
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            Conectar();
        }
        public void Conectar()
        {
            try
            {
                Equipo1.Connect();
                Equipo2.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void TmLectura_Tick(object sender, EventArgs e) //cada 100ms realiza lectura de entradas provenientes del controlador
        {
            float A1;
            int DI1, DI2, DI3, DI4,A2PLC;
            bool DO1;
            int i;//id inicial
            int MaxId = 5;//idmax = cant puntos "SELECT MAX(id) FROM puntos"
            try
            {
                EntradaAnalogica = Equipo1.ReadHoldingRegisters(29, 15);// puerto es una conecion tcp/ip 
                EntradaAnalogicaPLC = Equipo2.ReadHoldingRegisters(2, 1);
                EntradaDigital = Equipo2.ReadHoldingRegisters(200, 10);
                //PLCM221.WriteSingleCoil(20, false);
                A1 = EntradaDigital[0];
                DI2 = EntradaAnalogica[12];//la 9 es la fuenta direccion 400042
                DI3 = EntradaAnalogica[2];
                DI4 = EntradaAnalogica[3];
                A2PLC = EntradaAnalogicaPLC[0];

                // Variables a mostrar por pantalla
                ENCODER.Text = A1.ToString() + "RPM";
                VOLTAJE.Text = (DI2/10).ToString() + "V";
                TEMP.Text = DI3.ToString() + "°C";
                PRESS.Text = DI4.ToString() + "mBar";
                APERTURA.Text = A2PLC.ToString() + " %";

                // Cargamos valor de variables en la nube para ser leidas por APP Celular

                MySqlConnection myConnection = new MySqlConnection("server = **.**.**.***; Uid = *****; pwd = ******; database = ******");
                MySqlCommand myCommand = new MySqlCommand("REPLACE INTO variables(id, nombre, valor) values(1, VOLTAJE,'" + DI2 + "' )");
                myCommand.Connection = myConnection;
                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myCommand.Connection.Close();
                ENCODER.Text = A1.ToString() + "V";
                VOLTAJE.Text = (DI2/10).ToString() + "V";
                TEMP.Text = DI3.ToString() + "°C";
                PRESS.Text = DI4.ToString() + "mBar";
                
                // Verificamos que esten dentro de los limites normales, sino generamos alarma
                for (i = 1; i <= 2; i++)
                {
                    
                    verificarAlarma(i, "M221");
                }
            }
            catch (Exception ex)
            {
                
            }

        }

        public void verificarAlarma(int id, string equipo) //Verificamos que los puntos se encuentren dentro del rango de trabajo
        {
            string puerto = "", direccion = "", tag = "", alarmaHi = "", alarmaLow = "";
            int lectura;
           
            MySqlConnection myConnection = new MySqlConnection("server = **.**.**.***; Uid = *******; pwd = ******; database = ******");
            MySqlCommand myCommand = new MySqlCommand("SELECT * FROM puntos where id='" + id + "' and puerto='" + equipo + "'");
            myCommand.Connection = myConnection;
            myConnection.Open();
            myCommand.ExecuteNonQuery();
            MySqlDataReader reader = myCommand.ExecuteReader();
            if (reader.Read())
            {
                puerto = reader.GetString(1);
                direccion = reader.GetString(2);
                tag = reader.GetString(3);
                alarmaHi = reader.GetString(6);
                alarmaLow = reader.GetString(7);
                reader.Close();
            }

            int num_direccion = Int32.Parse(direccion);
            int num_alarmaHi = Int32.Parse(alarmaHi);
            int num_alarmaLow = Int32.Parse(alarmaLow);
            myCommand.Connection.Close();
            //lectura = EntradaAnalogica[id];
            lectura = EntradaAnalogicaPLC[id];

            if (lectura >= num_alarmaHi || lectura <= num_alarmaLow)
            {
                
                if (lectura != aux[id]) 
                {

                        MySqlCommand myCommand2 = new MySqlCommand("INSERT INTO alarmas (hora,tag,puerto,direccion) VALUES(now()-INTERVAL 3 hour,'" + tag + "','" + puerto + "','" + num_direccion + "')");
                        myCommand2.Connection = myConnection;
                        myConnection.Open();
                        myCommand2.ExecuteNonQuery();
                        myCommand2.Connection.Close();
                        AlarmaInd.Visible = true;
                        led = 1; //esta variable nos sirve para mostrar el led en las otras pantallas 
                        aux[id] = lectura; // alamaceno para que no se repita la alarma
                        myCommand2.Connection.Close();
                        
                }


            }
        }

        public void button2_Click(object sender, EventArgs e)
        {
            if (Login.User == 1)
            {
                ControlElectrico Co = new ControlElectrico();
                Co.Show();
                //this.Hide();
            }
            else
            {
                MessageBox.Show("Inicie Sesion para tener acceso al Control Eléctrico");
            }
           
        }

        public void button4_Click(object sender, EventArgs e)
        {
            AlarmaInd.Visible = false;
        }

        public void button3_Click(object sender, EventArgs e)
        {
            Alarmas Al = new Alarmas();
            Al.Show();
            //this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
           
                Config Co = new Config();
                Co.Show();
                //this.Hide();
            
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Alarmas Al = new Alarmas();
            Al.Show();
            //this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            valvMariposa.ForeColor = Color.Red;
            valvMariposa.Text = "CERRADO";

        }

        private void button7_Click(object sender, EventArgs e)
        {
            valvMariposa.ForeColor = Color.Lime;
            valvMariposa.Text = "ABIERTO";
        }
    }
}
