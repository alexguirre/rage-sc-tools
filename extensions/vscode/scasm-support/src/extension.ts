// based on https://github.com/microsoft/qsharp-compiler/blob/e1a43f3da021fc94d7214ea197ba3905ff96695c/src/VSCodeExtension/src/languageServer.ts
import * as fs from "fs";
import * as cp from "child_process";
import * as net from 'net';
import * as url from 'url';
import * as vscode from "vscode";
import * as vscode_lc from "vscode-languageclient";
import * as pf from "portfinder";

export async function activate(context: vscode.ExtensionContext) {
    console.log("SC extension activated");

    await startLanguageServer(context);
}

async function startLanguageServer(context: vscode.ExtensionContext) {
    const rootFolder = findRootFolder();

    const config = vscode.workspace.getConfiguration();
    const exePath = config.get("sclang.languageServerPath") as string;

    if (!(await isValidExe(exePath))) {
        console.log(`[sclang] sclang.languageServerPath = '${exePath}' is not an executable`);
        return;
    }

    const serverOptions = startServerOptions(exePath, rootFolder);

    const clientOptions: vscode_lc.LanguageClientOptions = {
        documentSelector: [
            {scheme: "file", language: "sclang"}
        ],
        uriConverters: {
            // VS Code by default %-encodes even the colon after the drive letter
            code2Protocol: uri => url.format(url.parse(uri.toString(true))),
            protocol2Code: str => vscode.Uri.parse(str)
        },
        errorHandler: {
            closed: () => {
                return vscode_lc.CloseAction.Restart;
            },
            error: (error, message, count) => {
                console.log(`[sclang] Client error. ${error.name}: ${error.message}`);
                // By default, continue the server as best as possible.
                return vscode_lc.ErrorAction.Continue;
            }
        }
    };

    const client = new vscode_lc.LanguageClient(
        "sclang",
        "ScLang",
        serverOptions,
        clientOptions,
        false
    );

    let disposable = client.start();
    context.subscriptions.push(disposable);

    console.log("[sclang] Started language client.");
}

function startServerOptions(exePath: string, cwd: string): vscode_lc.ServerOptions {
    return () => new Promise<vscode_lc.StreamInfo>((resolve, reject) => {
        const server = net.createServer(socket => {
            resolve({ reader: socket, writer: socket } as vscode_lc.StreamInfo);
        });
    
        const host = "127.0.0.1";
        pf.getPortPromise({ host: host })
        .then(port => {
            return listenPromise(server, port, port + 10, host);
        })
        .then(actualPort => {
            console.log(`[sclang] Listening to port ${actualPort}`);

            spawnProcess(exePath, cwd, actualPort);
        })
        .catch(err => {
            reject(err);
        });
    
    });
}

function listenPromise(server: net.Server, port: number, maxPort: number, hostname: string): Promise<number> {
    return new Promise((resolve, reject) => {
        if (port >= maxPort) {
            reject("Could not find port");
        }

        server.listen(port, hostname)
            .on("listening", () => resolve(port))
            .on("error", err => {
                if ("code" in err && (err as any).code === "EADDRINUSE") {
                    resolve(listenPromise(server, port + 1, maxPort, hostname));
                }

                reject(err);
            });
    });
}

function spawnProcess(exePath: string, cwd: string, port: number) {
    const process = cp.spawn(exePath, [`${port}`], { cwd: cwd }) // , "--wait-for-debugger"
        .on("error", err => {
            console.log(`[sclang] Language server process spawn failed with '${err}'`);
            throw err;
        })
        .on("exit", (code, signal) => console.log(`[sclang] Language server process exited with code ${code}`));

    process.stderr.on('data', (data) => {
        console.error(`[sclang-language-server] ${data}`);
    });
    process.stdout.on('data', (data) => {
        console.log(`[sclang-language-server] ${data}`);
    });

    console.log(`[sclang] Started language server process with PID ${process.pid}`);
}

function isValidExe(exePath: string) : Promise<boolean> {
    return new Promise((resolve, reject) => {
        if (exePath === undefined || exePath === null) {
            resolve(false);
        } else {
            fs.access(exePath, fs.constants.X_OK, err => {
                resolve(err === null);
            });
        }
    });
}

function findRootFolder() : string {
    const workspaces = vscode.workspace.workspaceFolders;
    if (workspaces) {
        return workspaces[0].uri.fsPath;
    } else {
        return "";
    }
}