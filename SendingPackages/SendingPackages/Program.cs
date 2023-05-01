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
        Console.Write("Enter the number of packages to send: ");
        int numberOfPackages;
        if (!int.TryParse(Console.ReadLine(), out numberOfPackages) || numberOfPackages <= 0)
        {
            Console.WriteLine("Invalid input. Please enter a positive integer.");
            return;
        }

        lostPackageId = new Random().Next(1, numberOfPackages + 1);

        var senderThread = new Thread(Sender);
        var receiverThread = new Thread(Receiver);

        senderThread.Start(numberOfPackages);
        receiverThread.Start();

        senderThread.Join();
        receiverThread.Join();

        Console.WriteLine("All packages have been sent and acknowledged.");
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
                ackReceivedForCurrentPackage = ackReceived.WaitOne(2000); // Wait for 2 seconds for an ACK

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
                lostPackageId = -1; // Mark the lost package as handled
                continue; // Do not send ACK for the lost package
            }

            Console.WriteLine($"Receiver: Received package {receivedPackage.Id} and sent ACK");
            ackReceived.Set();

            if (receivedPackage.Id == lostPackageId && lostPackageId == -1)
            {
                break; // Exit when the resent lost package has been received
            }
        }
    }
}
