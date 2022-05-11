using System;
using System.Numerics;
using System.Reflection;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ImGuiNET
{
    public class ImVeldridApp
    {
        public static ImVeldridApp current { get; private set; }


        private Sdl2Window _window;
        public Vector3 clearColor = new Vector3(0.45f, 0.55f, 0.6f);

        #region windowTitle

        string _windowTitle;
        public ImVeldridApp(string windowTitle = null)
        {
            _windowTitle = windowTitle ?? defaultWindowTitle;
        }

        public static string defaultWindowTitle
        {
            get
            {
                var appName = Assembly.GetEntryAssembly().GetName();
                return $"{appName.Name} - {appName.Version}";
            }
        }

        public string windowTitle
        {
            get => _window?.Title ?? _windowTitle;
            set
            {
                _windowTitle = value;
                if (_window != null)
                    _window.Title = value;
            }
        }

        #endregion

        public void Run(Action uiLoop)
        {
            current = this;

            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, windowTitle),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out var gd);

            var cl = gd.ResourceFactory.CreateCommandList();
            var controller = new ImGuiController(gd, _window, gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            _window.Resized += () =>
            {
                gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                controller.WindowResized(_window.Width, _window.Height);
            };

            // Main application loop
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                uiLoop();

                cl.Begin();
                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, 1f));
                controller.Render(gd, cl);
                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
                controller.SwapExtraWindows(gd);
            }

            // Clean up Veldrid resources
            gd.WaitForIdle();
            controller.Dispose();
            cl.Dispose();
            gd.Dispose();
        }
    }
}
