using Spectre.Console.Cli;
using IPFLang.CLI.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ipflang");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<ParseCommand>("parse")
        .WithDescription("Parse and validate an IPFLang script")
        .WithExample("parse", "examples/epo_filing.ipf");
    
    config.AddCommand<RunCommand>("run")
        .WithDescription("Execute an IPFLang script with inputs")
        .WithExample("run", "examples/epo_filing.ipf", "--interactive")
        .WithExample("run", "examples/epo_filing.ipf", "--inputs", "inputs.json");
    
    config.AddCommand<VerifyCommand>("verify")
        .WithDescription("Run verification checks (completeness, monotonicity)")
        .WithExample("verify", "examples/epo_filing.ipf");
    
    config.AddCommand<InfoCommand>("info")
        .WithDescription("Display information about a script (inputs, fees, groups)")
        .WithExample("info", "examples/epo_filing.ipf");
});

return app.Run(args);
