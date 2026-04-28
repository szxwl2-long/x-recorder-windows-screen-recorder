Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$form = New-Object System.Windows.Forms.Form
$form.Text = 'dodex'
$form.Size = New-Object System.Drawing.Size(900,600)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(15,23,42)
$label = New-Object System.Windows.Forms.Label
$label.Text = 'dodex test window'
$label.ForeColor = [System.Drawing.Color]::White
$label.Font = New-Object System.Drawing.Font('Segoe UI',24,[System.Drawing.FontStyle]::Bold)
$label.AutoSize = $true
$label.Location = New-Object System.Drawing.Point(40,40)
$form.Controls.Add($label)
$appLabel = New-Object System.Windows.Forms.Label
$appLabel.Text = 'Recording validation target'
$appLabel.ForeColor = [System.Drawing.Color]::FromArgb(203,213,225)
$appLabel.Font = New-Object System.Drawing.Font('Segoe UI',16)
$appLabel.AutoSize = $true
$appLabel.Location = New-Object System.Drawing.Point(44,100)
$form.Controls.Add($appLabel)
[System.Windows.Forms.Application]::Run($form)
