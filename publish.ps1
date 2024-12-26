
$build = (get-date).ToString("yyyyMMddHHmm");
$localBuild = "./dist/nehsanet/browser";
$version = (get-date).ToString("yyyy-MM-dd HH:mm");
$full_version = "<!-- ${version} -->";
$index_location = "$localBuild/index.html";

$val = 'both';
if ($val.ToLower() -eq '' -or $val.ToLower() -eq 'ui' -or $val.ToLower() -eq 'both') {
    set-location ../nehsa.net; 

    if ($val.ToLower() -eq 'both') {
        write-host "Updating version.ts";
        set-content -path "./src/version.ts" -value "export const version = { number: '$version' }";
    }
    else {
        $val = read-host "Apply build number ${version}? [Y/n]";
        if ($val.ToLower() -eq '' -or $val.ToLower() -eq 'y') {
            write-host "Updating version.ts";
            set-content -path "./src/version.ts" -value "export const version = { number: '$version' }";
        }
    }
    
    write-host "Removing old files at $localBuild";
    remove-item -v $localBuild/* -recurse;
    
    write-host "Running: ng build --configuration production (within WSL)";
    ng build --configuration production;

    write-host "Updating wwwroot with Angular app";    
    if (Test-Path $localBuild) {
        if (Test-Path ../nehsanet-web-app/wwwroot) {
            write-host "Delete ../nehsanet-web-app/wwwroot for fresh contents";    
            remove-item -path ../nehsanet-web-app/wwwroot/* -force -recurse
        }
        write-host "Copying ${localBuild}: copy-item -force $localBuild/* ../nehsanet-web-app/wwwroot";
        copy-item -force -recurse -v $localBuild/* ../nehsanet-web-app/wwwroot
    }

    write-host "Updating sitemap";
    if (Test-Path "./sitemap.xml") {
        remove-item ./sitemap.xml
    }
    set-location /mnt/c/src/nehsa/python-angular-sitemapper
    if (Test-Path "./sitemap.xml") {
        remove-item ./sitemap.xml
    }
    python3 update.py
    copy-item -v ./sitemap.xml /mnt/c/src/nehsa/nehsa.net/src/sitemap.xml
    set-location /mnt/c/src/nehsa/nehsa.net
    
    write-host "Updating index.html and version.ts with version info: ${version}";
    add-content -path $index_location -value $full_version;

    write-host "UI ready for deployment.";

    set-location ../nehsanet-web-app
}

write-host "Getting WEB ready...";
write-host "Building docker WEB image ${build}";
docker build . -t nehsa/nehsanet:$build --platform linux/amd64;
    
write-host "Pushing WEB image to DockerHub...";
docker push nehsa/nehsanet:$build;

$cmd = "docker run -p 80:80/tcp -p 443:443/tcp nehsa/nehsanet:${build}";
write-host 'To run:';
write-host $cmd;

# output to file
$cmd | out-file -filepath ./run-api.ps1;
write-host 'To run in the future: ./run-api.ps1';

write-host "Done!";
