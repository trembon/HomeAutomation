using HomeAutomation.Database.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class WeatherForecast
{
    [Key]
    [Required]
    public int ID { get; set; }

    public DateTime Date { get; set; }

    public WeatherForecastPeriod Period { get; set; }

    public string? WindDirection { get; set; }

    public double WindSpeed { get; set; }

    public double Temperature { get; set; }

    public double Rain { get; set; }

    /// <summary>
    /// PNG: http://symbol.yr.no/grafikk/sym/{size}/{id}.png
    /// Sizes: b30, b38, b48, b100, b200
    /// 
    /// SVG: http://symbol.yr.no/grafikk/sym/svg/{id}.svg
    /// </summary>
    public string? SymbolID { get; set; }
}
