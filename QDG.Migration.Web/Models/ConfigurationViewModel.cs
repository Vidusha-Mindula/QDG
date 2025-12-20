using System.ComponentModel.DataAnnotations;

namespace QDG_DB_Migrator.Models;

public class ConfigurationViewModel
{
    public int Id { get; set; }
    
    [Required]
    [Display(Name = "Configuration Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Source Host")]
    public string SourceHost { get; set; } = "localhost";
    
    [Required]
    [Display(Name = "Source Port")]
    public int SourcePort { get; set; } = 3306;
    
    [Required]
    [Display(Name = "Source Database")]
    public string SourceDatabase { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Source Username")]
    public string SourceUsername { get; set; } = "root";
    
    [Required]
    [Display(Name = "Source Password")]
    [DataType(DataType.Password)]
    public string SourcePassword { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Destination Host")]
    public string DestinationHost { get; set; } = "localhost";
    
    [Required]
    [Display(Name = "Destination Port")]
    public int DestinationPort { get; set; } = 3306;
    
    [Required]
    [Display(Name = "Destination Database")]
    public string DestinationDatabase { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Destination Username")]
    public string DestinationUsername { get; set; } = "root";
    
    [Required]
    [Display(Name = "Destination Password")]
    [DataType(DataType.Password)]
    public string DestinationPassword { get; set; } = string.Empty;
}
