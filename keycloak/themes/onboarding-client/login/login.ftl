<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=true; section>
    <#if section = "header">
        Entrar
    <#elseif section = "form">
        <div id="kc-form">
            <div id="kc-form-wrapper">
                <#if realm.password>
                    <form id="kc-form-login" onsubmit="return true;" action="${url.loginAction}" method="post" novalidate>
                        <!-- Username / Email -->
                        <div class="kc-form-group">
                            <label for="username" class="kc-label">
                                <#if !realm.loginWithEmailAllowed>
                                    ${msg("username")}
                                <#elseif !realm.registrationEmailAsUsername>
                                    ${msg("usernameOrEmail")}
                                <#else>
                                    ${msg("email")}
                                </#if>
                            </label>
                            <input
                                tabindex="1"
                                id="username"
                                class="kc-input"
                                name="username"
                                value="${(login.username!'')}"
                                type="text"
                                autofocus
                                autocomplete="off"
                                placeholder="<#if !realm.loginWithEmailAllowed>nome de usuário<#elseif !realm.registrationEmailAsUsername>email ou usuário<#else>seu@email.com</#if>"
                                aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                            />
                            <#if messagesPerField.existsError('username','password')>
                                <span class="kc-input-error" aria-live="polite">
                                    ${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}
                                </span>
                            </#if>
                        </div>

                        <!-- Password -->
                        <div class="kc-form-group">
                            <div class="kc-label-row">
                                <label for="password" class="kc-label">${msg("password")}</label>
                                <#if realm.resetPasswordAllowed>
                                    <a tabindex="5" class="kc-forgot-link" href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a>
                                </#if>
                            </div>
                            <div class="kc-input-wrapper">
                                <input
                                    tabindex="2"
                                    id="password"
                                    class="kc-input"
                                    name="password"
                                    type="password"
                                    autocomplete="off"
                                    placeholder="••••••••"
                                    aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                                />
                                <button class="kc-password-toggle" type="button" aria-label="${msg('showPassword')}"
                                        aria-controls="password" data-password-toggle
                                        data-icon-show="" data-icon-hide=""
                                        data-label-show="${msg('showPassword')}" data-label-hide="${msg('hidePassword')}">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="kc-eye-icon kc-eye-closed">
                                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                        <circle cx="12" cy="12" r="3"/>
                                    </svg>
                                </button>
                            </div>
                        </div>

                        <!-- Remember Me -->
                        <#if realm.rememberMe && !usernameEditDisabled??>
                            <div class="kc-form-group kc-remember-me">
                                <label class="kc-checkbox-label">
                                    <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox"
                                        <#if login.rememberMe??>checked</#if>
                                    />
                                    <span>${msg("rememberMe")}</span>
                                </label>
                            </div>
                        </#if>

                        <!-- Submit -->
                        <div class="kc-form-group kc-form-buttons">
                            <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>/>
                            <button tabindex="4" class="kc-button kc-button-primary" name="login" id="kc-login" type="submit">
                                ${msg("doLogIn")}
                            </button>
                        </div>
                    </form>
                </#if>
            </div>
        </div>
    <#elseif section = "info">
        <!-- Registration link — always visible for client portal -->
        <div class="kc-registration-section">
            <span>Não tem uma conta?</span>
            <a href="${properties.registrationUrl!'/register'}" class="kc-register-link">Criar conta &rarr;</a>
        </div>
    </#if>
</@layout.registrationLayout>
