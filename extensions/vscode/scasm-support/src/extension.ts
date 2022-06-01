// based on https://github.com/microsoft/qsharp-compiler/blob/e1a43f3da021fc94d7214ea197ba3905ff96695c/src/VSCodeExtension/src/languageServer.ts
import * as fs from "fs";
import * as cp from "child_process";
import * as net from 'net';
import * as vscode from "vscode";
import { LanguageClient, LanguageClientOptions, ServerOptions, TransportKind } from "vscode-languageclient/node";

export async function activate(context: vscode.ExtensionContext) {
    console.log("SC extension activated");

    await startLanguageServer(context);
}

async function startLanguageServer(context: vscode.ExtensionContext) {
    const config = vscode.workspace.getConfiguration();
    const exePath = config.get("sclang.languageServerPath") as string;

    if (!(await isValidExe(exePath))) {
        console.log(`[sclang] sclang.languageServerPath = '${exePath}' is not an executable`);
        return;
    }

    const serverOptions = getServerOptions(exePath);

    const clientOptions: LanguageClientOptions = {
        documentSelector: [
            { scheme: "file", language: "sclang" }
        ],
        markdown: {
            isTrusted: true,
        }
    };

    const client = new LanguageClient(
        "sclang",
        "ScLang",
        serverOptions,
        clientOptions
    );

    let disposable = client.start();
    context.subscriptions.push(disposable);

    console.log("[sclang] Started language client.");
}

function getServerOptions(exePath: string): ServerOptions {
    const serverOptions: ServerOptions = {
        run: {
            command: exePath,
            args: [],
            options: {
            },
        },
        debug: {
            command: exePath,
            args: ["--launch-debugger"],
            options: {
            },
        }
    };

    return serverOptions;
}

async function isValidExe(exePath: string) : Promise<boolean> {
    if (exePath === undefined || exePath === null) {
        return false;
    }

    try {
        await fs.promises.access(exePath, fs.constants.X_OK);
        return true;
    } catch {
        return false;
    }
}
