"""Compile against the installed Unity/reference assemblies; does not replace PlayMode tests."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
unity = Path('C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data')
packages = unity / 'Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.urp-blank-17.2.1/ScriptAssemblies'
output = root / 'Logs/Compile'
output.mkdir(parents=True, exist_ok=True)
references = list((unity / 'NetStandard/ref').rglob('*.dll'))
references += list((unity / 'NetStandard/compat/2.1.0/shims/netfx').glob('*.dll'))
references += list((unity / 'Managed/UnityEngine').glob('*.dll'))
references += [p for p in packages.glob('*.dll') if p.name.startswith(('Unity.', 'UnityEngine.TestRunner', 'UnityEditor.TestRunner'))]
references += [unity / 'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll']
csc = sorted(Path('C:/Program Files/dotnet/sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
messages = []
for name, folder in [('Palsoul.Runtime', 'Scripts'), ('Palsoul.Editor', 'Editor'), ('Palsoul.EditModeTests', 'Tests')]:
    assembly = output / (name + '.dll')
    args = ['-nologo', '-target:library', '-langversion:latest', '-define:UNITY_EDITOR,ENABLE_INPUT_SYSTEM',
            '-nowarn:0649', '-out:' + str(assembly)]
    args += ['-r:' + str(p) for p in references]
    args += [str(p) for p in (root / 'Assets/_Project' / folder).rglob('*.cs')]
    response = output / (name + '.rsp')
    response.write_text('\n'.join('"' + a + '"' for a in args), encoding='utf-8')
    result = subprocess.run(['dotnet', str(csc), '@' + str(response)], cwd=root, capture_output=True, text=True)
    message = f'{name}: exit {result.returncode}\n' + result.stdout + result.stderr
    print(message)
    messages.append(message)
    (output / 'result.txt').write_text('\n'.join(messages), encoding='utf-8')
    if result.returncode:
        raise SystemExit(result.returncode)
    references.append(assembly)
