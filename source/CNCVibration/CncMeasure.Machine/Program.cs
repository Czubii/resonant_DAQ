using CncMeasurement.Machine;

Console.WriteLine("--- Start stanowiska badawczego CNC ---");

// 1. Object instance of the machine controller, connecting to the CNC machine on COM3 with a baud rate of 115200
var cncController = new MachineController("COM3", 115200);
await cncController.ConnectAndUnlockAsync();

Console.WriteLine("Połączono z maszyną. Naciśnij [1], aby wykonać Sweep, [2] aby zmienić pozycję Y.");

// 2. Testing user input to run either the Sweep test or set a specific X position
string input = Console.ReadLine();

if (input == "1")
{
    Console.WriteLine("Rozpoczynam test Sweep...");
    await cncController.RunSweep();
    Console.WriteLine("Test Sweep zakończony!");
}
else if (input == "2")
{
    Console.WriteLine("Wpisz pozycję Y w milimetrach (np. 15,5):");
    if (double.TryParse(Console.ReadLine(), out double yPos))
    {
        await cncController.SetYPosition(yPos);
        Console.WriteLine("Przejazd wykonany.");
    }
    else
    {
        Console.WriteLine("Błędna wartość.");
    }
}

Console.WriteLine("Naciśnij dowolny klawisz, aby zamknąć...");
Console.ReadKey();

// 3. Bezpieczne zamknięcie portu i wrzeciona
cncController.Dispose();
