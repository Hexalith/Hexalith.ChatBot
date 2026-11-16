"""Require the shared environment and enable fail-closed live ChatBot product tests."""

import os
from pathlib import Path
import subprocess


def main() -> None:
    """Check existing tools without rebuilding or installing an environment."""
    subprocess.run(["docker", "info"], check=True, stdout=subprocess.DEVNULL)
    subprocess.run(["dapr", "--version"], check=True)
    # Resolve the Playwright-managed browser already installed by shared preparation.
    # This is the same package build output used by the live browser test project.
    driver = Path("tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/.playwright")
    environment = os.environ.copy()
    javascript = """
const fs = require('fs');
const {chromium} = require(process.argv[1]);
const executable = chromium.executablePath();
if (!fs.existsSync(executable)) throw new Error(`Required Chromium is unavailable: ${executable}`);
process.stdout.write(executable);
"""
    browser = subprocess.run(
        [str(driver / "node/linux-x64/node"), "-e", javascript, str((driver / "package").resolve())],
        check=True, capture_output=True, text=True, env=environment,
    ).stdout
    if not browser or "\n" in browser or "\r" in browser:
        raise ValueError("The shared Playwright build did not resolve one browser executable")
    subprocess.run([browser, "--version"], check=True)
    tool_root = subprocess.run(
        ["dotnet", "msbuild", "src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj",
         "-p:Configuration=Release", "-p:UseHexalithProjectReferences=false",
         "-getProperty:_ChatBotPublishedToolRoot"],
        check=True, capture_output=True, text=True,
    ).stdout.strip()
    if not tool_root or "\n" in tool_root or "\r" in tool_root:
        raise ValueError("The AppHost did not resolve one published composition tool directory")
    if not (Path(tool_root) / "Hexalith.Builds.Tooling.dll").is_file():
        raise FileNotFoundError("The manifest-pinned published composition tool was not restored")
    with Path(os.environ["GITHUB_ENV"]).open("a", encoding="utf-8") as stream:
        stream.write("HEXALITH_CHATBOT_TIER3=1\nHEXALITH_CHATBOT_TIER3_REQUIRED=1\n")
        stream.write(f"CHROME_EXECUTABLE_PATH={browser}\n")
        stream.write(f"HexalithBuildsPublishedToolRoot={tool_root}\n")
        stream.write("Configuration=Release\nUseHexalithProjectReferences=false\n")
        stream.write("HexalithEventStoreFromSource=false\nShouldUnsetParentConfigurationAndPlatform=false\n")


if __name__ == "__main__":
    main()
