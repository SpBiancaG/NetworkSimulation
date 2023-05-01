using System;
using System.Collections.Generic;
using System.Threading;

class Package
{
    public int Id { get; set; }
}

class Program
{
    private static readonly Queue<Package> packageQueue = new Queue<Package>();
    private static readonly AutoResetEvent packageSent = new AutoResetEvent(false);
    private static readonly AutoResetEvent ackReceived = new AutoResetEvent(false);
    private static int lostPackageId = -1;

    static void Main()
    {
        Console.Write("Numărul de pachete ce trebuie trimise: ");
        int numberOfPackages;
        if (!int.TryParse(Console.ReadLine(), out numberOfPackages) || numberOfPackages <= 0)
        {
            Console.WriteLine("Input invalid");
            return;
        }

        lostPackageId = new Random().Next(1, numberOfPackages + 1);

        var senderThread = new Thread(Sender);
        var receiverThread = new Thread(Receiver);

        senderThread.Start(numberOfPackages);
        receiverThread.Start();

        senderThread.Join();
        receiverThread.Join();

        
    }

    static void Sender(object obj)
    {
        int numberOfPackages = (int)obj;

        for (int i = 1; i <= numberOfPackages; i++)
        {
            bool ackReceivedForCurrentPackage = false;
            while (!ackReceivedForCurrentPackage)
            {
                var package = new Package { Id = i };
                lock (packageQueue)
                {
                    packageQueue.Enqueue(package);
                    Console.WriteLine($"Sender: Sent package {package.Id}");
                }

                packageSent.Set();
                ackReceivedForCurrentPackage = ackReceived.WaitOne(2000); // se face timeout ul dupa 2 secunde

                if (!ackReceivedForCurrentPackage)
                {
                    Console.WriteLine($"Sender: No ACK received for package {package.Id}. Resending...");
                }
            }
        }
    }

    static void Receiver()
    {
        while (true)
        {
            packageSent.WaitOne();

            Package receivedPackage;
            lock (packageQueue)
            {
                receivedPackage = packageQueue.Dequeue();
            }

            if (receivedPackage.Id == lostPackageId)
            {
                Console.WriteLine($"Receiver: Package {receivedPackage.Id} was lost");
                lostPackageId = -1; // flag pentru un pachet pierdut dar handle-uit 
                continue; // nu transmitem ack pentru un pachet pierdut
            }

            Console.WriteLine($"Receiver: Received package {receivedPackage.Id} and sent ACK");
            ackReceived.Set();

            if (receivedPackage.Id == lostPackageId && lostPackageId == -1)
            {
                break; // se iese atunci cand un pachet pierdut a fost retrimis
            }
        }
    }
}
