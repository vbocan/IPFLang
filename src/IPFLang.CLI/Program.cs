using Spectre.Console.Cli;
using IPFLang.CLI.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ipflang");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<ParseCommand>("parse")
        .WithDescription("Parse and validate an IPFLang script")
        .WithExample("parse", "examples/01_epo_filing.ipf");
    
    config.AddCommand<RunCommand>("run")
        .WithDescription("Execute an IPFLang script. Options: --inputs <file>, --provenance, --counterfactuals")
        .WithExample("run", "examples/01_epo_filing.ipf")
        .WithExample("run", "examples/01_epo_filing.ipf", "--inputs", "inputs.json")
        .WithExample("run", "examples/01_epo_filing.ipf", "--provenance")
        .WithExample("run", "examples/01_epo_filing.ipf", "--counterfactuals");
    
    config.AddCommand<VerifyCommand>("verify")
        .WithDescription("Run verification checks (completeness, monotonicity)")
        .WithExample("verify", "examples/01_epo_filing.ipf");
    
    config.AddCommand<InfoCommand>("info")
        .WithDescription("Display script metadata (inputs, fees, groups, version)")
        .WithExample("info", "examples/01_epo_filing.ipf");

    config.AddCommand<ComposeCommand>("compose")
        .WithDescription("Compose multiple IPFLang scripts with jurisdiction inheritance")
        .WithExample("compose", "base.ipf", "child.ipf")
        .WithExample("compose", "epo_base.ipf", "epo_de.ipf", "--analysis")
        .WithExample("compose", "epo_base.ipf", "epo_de.ipf", "--provenance");
});

return app.Run(args);
