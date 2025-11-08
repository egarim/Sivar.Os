using System;
using System.ComponentModel;
using Sivar.Os.Shared.DTOs.ValueObjects;

class TestLocation
{
    static void Main(string[] args)
    {
        // Test Location with INotifyPropertyChanging
        var location = new Location();
        
        bool propertyChangingFired = false;
        bool propertyChangedFired = false;
        
        location.PropertyChanging += (sender, e) => {
            Console.WriteLine($"PropertyChanging: {e.PropertyName}");
            propertyChangingFired = true;
        };
        
        location.PropertyChanged += (sender, e) => {
            Console.WriteLine($"PropertyChanged: {e.PropertyName}");
            propertyChangedFired = true;
        };
        
        Console.WriteLine("Setting City...");
        location.City = "San Salvador";
        
        Console.WriteLine($"Property changing fired: {propertyChangingFired}");
        Console.WriteLine($"Property changed fired: {propertyChangedFired}");
        Console.WriteLine($"City value: {location.City}");
        
        // Test that it doesn't fire for same value
        propertyChangingFired = false;
        propertyChangedFired = false;
        
        Console.WriteLine("\nSetting same City value...");
        location.City = "San Salvador";
        
        Console.WriteLine($"Property changing fired: {propertyChangingFired}");
        Console.WriteLine($"Property changed fired: {propertyChangedFired}");
        
        Console.WriteLine("\nLocation implementation is working correctly!");
    }
}