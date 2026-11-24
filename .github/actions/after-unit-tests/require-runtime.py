"""Require the shared environment and enable fail-closed live ChatBot product tests."""

import os
from pathlib import Path
import platform
import shlex
import subprocess
import sys


def playwright_node(driver: Path, system: str, architecture: str) -> Path:
    """Resolve the Node executable bundled with Playwright for this runner."""
    host = (system, architecture.lower())
    platform_directory = {
        ("Linux", "x86_64"): "linux-x64",
        ("Linux", "aarch64"): "linux-arm64",
        ("Linux", "arm64"): "linux-arm64",
        ("Darwin", "x86_64"): "darwin-x64",
        ("Darwin", "arm64"): "darwin-arm64",
        ("Windows", "amd64"): "win32_x64",
        ("Windows", "x86_64"): "win32_x64",
        ("Windows", "arm64"): "win32_x64",
    }.get(host)
    if platform_directory is None:
        raise ValueError(f"Playwright does not bundle a Node executable for {system} {architecture}")
    return driver / "node" / platform_directory / ("node.exe" if system == "Windows" else "node")


def main() -> None:
    """Check existing tools without rebuilding or installing an environment."""
    subprocess.run(["docker", "info"], check=True, stdout=subprocess.DEVNULL)
    subprocess.run(["dapr", "--version"], check=True, stdout=sys.stderr)
    # Resolve the Playwright-managed browser already installed by shared preparation.
    # This is the same package build output used by the live browser test project.
    driver = Path("tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net11.0/.playwright")
    environment = os.environ.copy()
    javascript = """
const fs = require('fs');
const {chromium} = require(process.argv[1]);
const executable = chromium.executablePath();
if (!fs.existsSync(executable)) throw new Error(`Required Chromium is unavailable: ${executable}`);
process.stdout.write(executable);
"""
    browser = subprocess.run(
        [str(playwright_node(driver, platform.system(), platform.machine())),
         "-e", javascript, str((driver / "package").resolve())],
        check=True, capture_output=True, text=True, env=environment,
    ).stdout
    if not browser or "\n" in browser or "\r" in browser:
        raise ValueError("The shared Playwright build did not resolve one browser executable")
    subprocess.run([browser, "--version"], check=True, stdout=sys.stderr)
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

    settings = {
        "HEXALITH_CHATBOT_TIER3": "1",
        "HEXALITH_CHATBOT_TIER3_REQUIRED": "1",
        "CHROME_EXECUTABLE_PATH": browser,
        "HexalithBuildsPublishedToolRoot": tool_root,
        "Configuration": "Release",
        "UseHexalithProjectReferences": "false",
        "HexalithEventStoreFromSource": "false",
        "ShouldUnsetParentConfigurationAndPlatform": "false",
    }
    if github_env := os.environ.get("GITHUB_ENV"):
        with Path(github_env).open("a", encoding="utf-8") as stream:
            for key, value in settings.items():
                stream.write(f"{key}={value}\n")
    else:
        for key, value in settings.items():
            print(f"export {key}={shlex.quote(value)}")


if __name__ == "__main__":
    main()
