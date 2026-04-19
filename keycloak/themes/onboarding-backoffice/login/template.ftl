<#macro registrationLayout bodyClass="" displayInfo=false displayMessage=true displayRequiredFields=false>
<!DOCTYPE html>
<html lang="${(locale.currentLanguageTag)!'pt-BR'}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="robots" content="noindex, nofollow">
    <title>${msg("loginTitle",(realm.displayName!''))}</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <#if properties.stylesCommon?has_content>
        <#list properties.stylesCommon?split(' ') as style>
            <link href="${url.resourcesCommonPath}/${style}" rel="stylesheet" />
        </#list>
    </#if>
    <#if properties.styles?has_content>
        <#list properties.styles?split(' ') as style>
            <link href="${url.resourcesPath}/${style}" rel="stylesheet" />
        </#list>
    </#if>
</head>
<body class="${bodyClass}">
    <!-- Top accent bar -->
    <div class="kc-accent-bar" aria-hidden="true"></div>

    <div class="kc-login-container">
        <div class="kc-login-card">
            <!-- Card Header -->
            <div class="kc-card-header">
                <div class="kc-admin-badge">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="18" height="11" x="3" y="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>
                    <span>Área Administrativa</span>
                </div>
                <h1 class="kc-card-title"><#nested "header"></h1>
                <#if properties.kcCardDescription?has_content>
                    <p class="kc-card-description">${properties.kcCardDescription}</p>
                </#if>
            </div>

            <!-- Card Content -->
            <div class="kc-card-content">
                <#if displayMessage && message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
                    <div class="kc-alert kc-alert-${message.type}">
                        <#if message.type = 'error'>
                            <svg class="kc-alert-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                        <#elseif message.type = 'success'>
                            <svg class="kc-alert-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
                        <#else>
                            <svg class="kc-alert-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>
                        </#if>
                        <span class="kc-alert-text">${kcSanitize(message.summary)?no_esc}</span>
                    </div>
                </#if>

                <#nested "form">
            </div>

            <!-- Card Footer (info section) -->
            <#if displayInfo>
                <div class="kc-card-footer">
                    <#nested "info">
                </div>
            </#if>
        </div>

        <!-- Powered by (optional branding) -->
        <p class="kc-footer-text">
            ${msg("loginTitleHtml",(realm.displayNameHtml!''))?no_esc}
        </p>
    </div>

    <#if scripts??>
        <#list scripts as script>
            <script src="${script}" type="text/javascript"></script>
        </#list>
    </#if>
    <script>
    // Password visibility toggle
    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('[data-password-toggle]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var input = document.getElementById(btn.getAttribute('aria-controls'));
                if (!input) return;
                var isPassword = input.type === 'password';
                input.type = isPassword ? 'text' : 'password';
                var icon = btn.querySelector('i');
                if (icon) {
                    icon.className = isPassword
                        ? btn.getAttribute('data-icon-hide')
                        : btn.getAttribute('data-icon-show');
                }
                btn.setAttribute('aria-label',
                    isPassword
                        ? btn.getAttribute('data-label-hide')
                        : btn.getAttribute('data-label-show')
                );
            });
        });
    });
    </script>
</body>
</html>
</#macro>
