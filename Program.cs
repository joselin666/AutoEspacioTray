using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoEspacio
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon iconoBandeja;
        private System.Windows.Forms.Timer temporizador;
        private System.Windows.Forms.Timer temporizadorPuntero;
        private CursorCountdownForm indicadorCuentaAtras;
        private int segundosRestantes = 60;

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const int VK_SPACE = 0x20;
        const int KEYEVENTF_KEYUP = 0x0002;

        public TrayApplicationContext()
        {
            // 1. Configurar el icono en la barra de tareas
            iconoBandeja = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath), 
                Text = "Auto-Espacio (Activo)",
                Visible = true
            };

            // 2. Crear los elementos del menú
            ToolStripMenuItem menuPausa = new ToolStripMenuItem("Pausar ejecución", null, AlternarPausa);
            ToolStripMenuItem menuCerrar = new ToolStripMenuItem("Cerrar programa", null, CerrarAplicacion);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(menuPausa);
            menu.Items.Add(new ToolStripSeparator()); // Añade una línea horizontal separadora
            menu.Items.Add(menuCerrar);
            
            iconoBandeja.ContextMenuStrip = menu;

            // 3. Actualizar la cuenta atrás cada segundo
            temporizador = new System.Windows.Forms.Timer();
            temporizador.Interval = 1000;
            temporizador.Tick += Temporizador_Tick;
            temporizador.Start();

            indicadorCuentaAtras = new CursorCountdownForm();
            indicadorCuentaAtras.Actualizar(segundosRestantes, true);
            indicadorCuentaAtras.Show();
            ActualizarPosicionIndicador();

            temporizadorPuntero = new System.Windows.Forms.Timer();
            temporizadorPuntero.Interval = 50;
            temporizadorPuntero.Tick += (_, _) => ActualizarPosicionIndicador();
            temporizadorPuntero.Start();
        }

        // Nueva función que controla la pausa y reanudación
        private void AlternarPausa(object? sender, EventArgs e)
        {
            // Obtenemos el botón del menú que fue pulsado
            if (sender is ToolStripMenuItem menuItem)
            {
                if (temporizador.Enabled)
                {
                    // Si estaba funcionando, lo detenemos
                    temporizador.Stop();
                    menuItem.Text = "Reanudar ejecución";
                    ActualizarIndicador();
                }
                else
                {
                    // Si estaba pausado, lo arrancamos de nuevo
                    temporizador.Start();
                    menuItem.Text = "Pausar ejecución";
                    ActualizarIndicador();
                }
            }
        }

        private void Temporizador_Tick(object? sender, EventArgs e)
        {
            segundosRestantes--;
            if (segundosRestantes > 0)
            {
                ActualizarIndicador();
                return;
            }

            keybd_event(VK_SPACE, 0, 0, UIntPtr.Zero);
            keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            segundosRestantes = 60;
            ActualizarIndicador();
        }

        private void ActualizarIndicador()
        {
            bool activo = temporizador.Enabled;
            indicadorCuentaAtras.Actualizar(segundosRestantes, activo);
            iconoBandeja.Text = activo ? "Auto-Espacio (Activo)" : "Auto-Espacio (Pausado)";
        }

        private void ActualizarPosicionIndicador()
        {
            indicadorCuentaAtras.ActualizarPosicion(Cursor.Position);
        }

        private void CerrarAplicacion(object? sender, EventArgs e)
        {
            iconoBandeja.Visible = false;
            temporizador.Stop();
            temporizadorPuntero.Stop();
            indicadorCuentaAtras.Close();
            Application.Exit();
        }
    }

    internal sealed class CursorCountdownForm : Form
    {
        private readonly Label texto;

        public CursorCountdownForm()
        {
            texto = new Label
            {
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(6),
                Font = new Font("Segoe UI", 9)
            };

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = texto.BackColor;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Controls.Add(texto);
        }

        public void Actualizar(int segundosRestantes, bool activo)
        {
            texto.Text = activo ? $"{segundosRestantes} s" : $"Pausado: {segundosRestantes} s";
        }

        public void ActualizarPosicion(Point posicionPuntero)
        {
            Rectangle area = Screen.FromPoint(posicionPuntero).WorkingArea;
            int x = posicionPuntero.X + 18;
            int y = posicionPuntero.Y + 22;

            if (x + Width > area.Right)
                x = posicionPuntero.X - Width - 12;
            if (y + Height > area.Bottom)
                y = posicionPuntero.Y - Height - 12;

            Location = new Point(
                Math.Max(area.Left, Math.Min(x, area.Right - Width)),
                Math.Max(area.Top, Math.Min(y, area.Bottom - Height)));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x00000020;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_TOPMOST = 0x00000008;
                const int WS_EX_NOACTIVATE = 0x08000000;

                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;

            if (message.Msg == WM_NCHITTEST)
            {
                message.Result = (IntPtr)HTTRANSPARENT;
                return;
            }

            base.WndProc(ref message);
        }
    }
}