#!/usr/bin/env python3
"""Fresh-process local restore test; fictional data only, no API/cloud/config providers."""
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory(prefix='coachos-keyring-proof-') as directory:
    temporary = Path(directory)
    home=temporary/'home'; home.mkdir()
    env={key:value for key,value in os.environ.items() if key in {'PATH','LD_LIBRARY_PATH','NUGET_PACKAGES','DOTNET_ROOT'}}
    env.update(HOME=str(home),DOTNET_CLI_HOME=str(home),DOTNET_CLI_TELEMETRY_OPTOUT='1')
    dotnet=shutil.which('dotnet')
    if not dotnet: raise SystemExit('dotnet missing')
    project=root/'scripts/tests/KeyringProbe/KeyringProbe.csproj'
    def run(command, expected=0):
        result=subprocess.run([str(arg) for arg in command],env=env,capture_output=True,text=True)
        if result.returncode != expected:
            raise RuntimeError(f'Local command failed (expected {expected}, got {result.returncode}): {result.stdout}\n{result.stderr}')
    run([dotnet,'build',project,'--verbosity','quiet'])
    dll=project.parent/'bin/Debug/net10.0/KeyringProbe.dll'
    ring=temporary/'source'; cipher=temporary/'fictional-ciphertext'; archive=temporary/'snapshot.zip'; restored=temporary/'restored'
    run([dotnet,dll,'protect',ring,cipher])
    # The writer has exited. --quiesced is now true, not a simulated assertion.
    run([sys.executable,root/'scripts/keyring_snapshot.py','backup',ring,archive,'--quiesced'])
    shutil.rmtree(ring)
    run([dotnet,dll,'unprotect',temporary/'wrong-ring',cipher],3)
    run([sys.executable,root/'scripts/keyring_snapshot.py','restore',archive,restored])
    run([dotnet,dll,'unprotect',restored,cipher])
    run([dotnet,dll,'unprotect',restored,cipher,'WrongApplication'],3)
    print('PASS: real DataProtectionTokenProtector in fresh processes; original ring removed; restored ring decrypts fictional payload; wrong ring/application rejected. No cloud/production.')
