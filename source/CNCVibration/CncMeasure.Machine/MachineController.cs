using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CncMeasurement.Core.Interfaces;

namespace CncMeasurement.Machine
{
    public class MachineController : IMachineController, IDisposable
    {
        private SerialPort _serialPort;
        private readonly SemaphoreSlim _responseSemaphore = new SemaphoreSlim(0);
        private volatile bool _isInitialized = false;
        private readonly object _serialPortLock = new object();

        // Ścieżka do katalogu, gdzie będą zapisywane tymczasowe pliki .gcode
        private readonly string _gcodeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedGCode");

        public MachineController(string portName = "COM3", int baudRate = 115200)
        {
            //Directory.CreateDirectory(_gcodeDirectory);
            //InitializeSerialPort(portName, baudRate);
        }

        private void InitializeSerialPort(string portName, int baudRate)
        {
            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                NewLine = "\r\n"
            };
            _serialPort.DataReceived += SerialPort_DataReceived;

            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FATAL ERROR] Błąd portu COM: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (_serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                string response = _serialPort.ReadLine().Trim();

                if (response.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    // --- WIZUALIZACJA W KONSOLI ---
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[CNC -> C#] Odpowiedź: ok (Maszyna przetworzyła linię)");
                    Console.ResetColor();
                    // ------------------------------

                    if (_responseSemaphore.CurrentCount == 0)
                    {
                        _responseSemaphore.Release();
                    }
                }
                else if (response.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CNC -> C#] BŁĄD SKŁADNI GRBL: {response}");
                    Console.ResetColor();

                    if (_responseSemaphore.CurrentCount == 0)
                    {
                        _responseSemaphore.Release();
                    }
                }
            }
        }
        /// <summary>
        /// Uruchomienie wrzeciona ze stałymi obrotami przez określony czas.
        /// Uwaga: Ponieważ sygnatura interfejsu zwraca void, metoda ta uruchamia proces asynchronicznie w tle (fire-and-forget).
        /// </summary>
        public void RunContinous(int RPM, float DurationSeconds)
        {
            if (!_isInitialized) return;

            Task.Run(async () =>
            {
                string filePath = Path.Combine(_gcodeDirectory, "continuous_run.gcode");

                // Szablon dla pracy ciągłej wrzeciona (GRBL akceptuje kropkę w G4 P jako sekundy)
                StringBuilder gcode = new StringBuilder();
                gcode.AppendLine("; --- Continuous Run Template ---");
                gcode.AppendLine("G21 ; Jednostki mm");
                gcode.AppendLine("G90 ; Pozycjonowanie absolutne");
                gcode.AppendLine($"M3 S{RPM} ; Włącz wrzeciono, ustaw obroty");
                gcode.AppendLine($"G4 P{DurationSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} ; Odczekaj czas pomiaru");
                gcode.AppendLine("M5 ; Wyłącz wrzeciono");

                await File.WriteAllTextAsync(filePath, gcode.ToString());
                await StreamGCodeFileAsync(filePath);
            });
        }

        /// <summary>
        /// Wykonuje profilowany sweep (np. krokowa zmiana obrotów wrzeciona dla testów rezonansowych).
        /// </summary>
        public async Task RunSweep()
        {
            if (!_isInitialized) return;

            string filePath = Path.Combine(_gcodeDirectory, "sweep_run.gcode");

            // Szablon generowania sweepu częstotliwości 
            StringBuilder gcode = new StringBuilder();
            gcode.AppendLine("; --- Sweep Run Template ---");
            gcode.AppendLine("G21");
            gcode.AppendLine("G90");

            int startRpm = 3000;
            int endRpm = 10000;
            int stepRpm = 100;
            float stepDuration = 1.0f; // 1 sekunda na każdy krok częstotliwości

            for (int rpm = endRpm; rpm >= startRpm; rpm -= stepRpm)
            {
                gcode.AppendLine($"M3 S{rpm} ; Zmiana stopnia wymuszenia");
                gcode.AppendLine($"G4 P{stepDuration.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
            }

            gcode.AppendLine("M5 ; Zakończenie testu");

            await File.WriteAllTextAsync(filePath, gcode.ToString());
            await StreamGCodeFileAsync(filePath);
        }

        /// <summary>
        /// Ustawienie pozycji osi Y (np. zmiana punktu przyłożenia czujnika/wymuszenia na ramie)
        /// </summary>
        public async Task SetYPosition(double yPosition)
        {
            if (!_isInitialized) return;

            string filePath = Path.Combine(_gcodeDirectory, $"move_Y_{yPosition}.gcode");

            StringBuilder gcode = new StringBuilder();
            gcode.AppendLine("; --- Move Y Template ---");
            gcode.AppendLine("G21");
            gcode.AppendLine("G90");
            // F1200 - posuw (feedrate) dostosowany do dynamiki konstrukcji
            gcode.AppendLine($"G1 Y{yPosition.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} F1200");

            await File.WriteAllTextAsync(filePath, gcode.ToString());
            await StreamGCodeFileAsync(filePath);
        }

        /// <summary>
        /// Rdzeń parsera: czyta plik linia po linii i wysyła do GRBL w protokole Call-and-Response
        /// </summary>
        private async Task StreamGCodeFileAsync(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                int lineNumber = 0;
                string lastSpindleState = ""; // Pamięć podręczna ostatnich obrotów

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string cleanLine = RemoveCommentsAndWhitespace(line);
                    if (string.IsNullOrEmpty(cleanLine)) continue;

                    // Zapamiętujemy komendę włączenia wrzeciona, by wiedzieć jak je przywrócić
                    if (cleanLine.StartsWith("M3", StringComparison.OrdinalIgnoreCase))
                    {
                        lastSpindleState = cleanLine;
                    }

                    lineNumber++;
                    bool success = false;

                    // Pętla będzie kręcić tak długo, aż TA KONKRETNA linijka zostanie zaakceptowana
                    while (!success)
                    {
                        try
                        {
                            while (_responseSemaphore.CurrentCount > 0)
                                await _responseSemaphore.WaitAsync();

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"[C# -> CNC] Linia {lineNumber}: {cleanLine}");
                            Console.ResetColor();

                            // Próba wysłania komendy - to tutaj wywali błąd, jeśli USB "umarło"
                            _serialPort.Write(cleanLine + "\n");

                            // Czekamy na 'ok'
                            bool receivedOk = await _responseSemaphore.WaitAsync(TimeSpan.FromSeconds(10));

                            if (receivedOk)
                            {
                                success = true; // Udało się, idziemy do kolejnej linijki G-code
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"[TIMEOUT] Maszyna nie odpowiedziała na linię {lineNumber}.");
                                Console.ResetColor();

                                // Odpalamy procedurę ratunkową
                                await HardReconnectAndResumeAsync(lastSpindleState);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ten blok łapie twarde błędy sprzętowe (np. zerwanie USB od EMI)
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[BŁĄD SPRZĘTOWY] Utracono komunikację: {ex.Message}");
                            Console.ResetColor();

                            // Odpalamy procedurę ratunkową
                            await HardReconnectAndResumeAsync(lastSpindleState);
                        }
                    }
                }
            }
        }
        private async Task HardReconnectAndResumeAsync(string lastSpindleCommand)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n[RECOVERY] Wykryto zerwanie połączenia! Rozpoczynam procedurę ratunkową...");
            Console.ResetColor();

            // 1. Brutalne zamknięcie zwieszonego portu
            try
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
            }
            catch { /* Ignorujemy błędy przy wymuszonym zamykaniu */ }

            // 2. Chwila oddechu na opadnięcie szpilek napięciowych na kablu USB
            await Task.Delay(2000);

            // 3. Ponowne otwarcie portu
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[RECOVERY FATAL] Nie można otworzyć portu COM: {ex.Message}");
                Console.ResetColor();
                throw; // Zatrzymujemy, jeśli kabel został fizycznie wyrwany
            }

            // 4. Inicjalizacja (Soft Reset + Kill Alarm z naszej wcześniejszej metody)
            await ConnectAndUnlockAsync();

            // 5. Przywrócenie stanu maszyny (np. włączenie wrzeciona, które zgasło po restarcie)
            if (!string.IsNullOrEmpty(lastSpindleCommand))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[RECOVERY] Przywracam stan maszyny komendą: {lastSpindleCommand}");
                Console.ResetColor();

                _serialPort.Write(lastSpindleCommand + "\n");
                await Task.Delay(2000); // Dajemy wrzecionu 2 sekundy na ponowne wejście na obroty
                _serialPort.DiscardInBuffer(); // Czyścimy ewentualne śmieci z bufora
            }
        }
        private string RemoveCommentsAndWhitespace(string line)
        {
            int commentIndex = line.IndexOf(';');
            if (commentIndex >= 0)
            {
                line = line.Substring(0, commentIndex);
            }
            return line.Trim();
        }

        public void Dispose()
        {
            lock (_serialPortLock)
            {
                if (_serialPort != null)
                {
                    try
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Write("M5\n"); // Zabezpieczenie: wyłączenie wrzeciona przy zamykaniu aplikacji
                            _serialPort.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Błąd podczas zamykania portu: {ex.Message}");
                    }
                    finally
                    {
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                }
            }
            _responseSemaphore?.Dispose();
        }

        public async Task ConnectAndUnlockAsync()
        {
            if (!_serialPort.IsOpen)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[BŁĄD] Port COM nie jest otwarty. Sprawdź kabel!");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INIT] Krok 1: Wykonuję sprzętowy Soft Reset (0x18)...");
            Console.ResetColor();

            // Znak 0x18 (Ctrl+X) to wbudowane w GRBL polecenie natychmiastowego resetu
            // Działa z pominięciem bufora G-code, prosto na mikrokontroler
            byte[] softResetCommand = new byte[] { 0x18 };
            _serialPort.Write(softResetCommand, 0, 1);

            // Dajemy mikrokontrolerowi czas na restart i wysłanie komunikatu powitalnego
            await Task.Delay(1500);

            // Czyścimy bufor wejściowy ze śmieci i powitań, których nie chcemy parsować
            _serialPort.DiscardInBuffer();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INIT] Krok 2: Odblokowuję maszynę - Kill Alarm ($X)...");
            Console.ResetColor();

            // Wysłanie komendy odblokowującej
            _serialPort.Write("$X\n");

            // Czekamy chwilę na przetworzenie i zwrotne "ok" z maszyny
            await Task.Delay(500);

            _isInitialized = true;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[INIT] Sukces! Maszyna zresetowana i gotowa do pracy.");
            Console.ResetColor();
        }
    }
}