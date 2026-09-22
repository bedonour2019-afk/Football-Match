// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification// for details on configuring this project to bundle and minify static web assets./**
 * Football Match App - Global JavaScript Utilities
 */let toastTimeout = null;document.addEventListener("DOMContentLoaded", function () {
    // 1. تهيئة الـ Tooltips من Bootstrap تلقائياً إن وجدت
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // 2. إغلاق القوائم والنوافذ عند الضغط على مفتاح Escape
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closeGlobalLightbox();
            const emojiPicker = document.getElementById("emojiPicker");
            if (emojiPicker) emojiPicker.classList.remove("show");

            const sidebar = document.getElementById("sidebar");
            if (sidebar && sidebar.classList.contains("show-mobile")) {
                toggleMobileSidebar();
            }
        }
    });});/**
 * عرض إشعار سريع (Toast Notification) في منتصف الشاشة
 * @param {string} message - نص الإشعار
 * @param {string} type - نوع الإشعار ('success', 'error', 'info')
 */function showToast(message, type = 'success') {
    let toastEl = document.getElementById("globalToastMsg");
    if (!toastEl) {
        toastEl = document.createElement("div");
        toastEl.id = "globalToastMsg";
        toastEl.style.cssText = `
            position: fixed;
            top: 20px;
            left: 50%;
            transform: translateX(-50%);
            padding: 10px 22px;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 600;
            color: #fff;
            z-index: 10000;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.35);
            transition: opacity 0.3s ease, transform 0.3s ease;
            opacity: 0;
            pointer-events: none;
            direction: rtl;
        `;
        document.body.appendChild(toastEl);
    }

    // إلغاء التوقيت السابق لمنع التداخل عند الضغط المتكرر
    if (toastTimeout) {
        clearTimeout(toastTimeout);
    }

    if (type === 'error') {
        toastEl.style.background = 'rgba(239, 68, 68, 0.95)';
    } else if (type === 'info') {
        toastEl.style.background = 'rgba(59, 130, 246, 0.95)';
    } else {
        toastEl.style.background = 'rgba(124, 58, 237, 0.95)';
    }

    toastEl.textContent = message;
    toastEl.style.opacity = '1';
    toastEl.style.transform = 'translateX(-50%) translateY(5px)';

    toastTimeout = setTimeout(() => {
        toastEl.style.opacity = '0';
        toastEl.style.transform = 'translateX(-50%) translateY(0px)';
    }, 2500);}/**
 * نسخ النص إلى الحافظة مع توافقية متقدمة للمتصفحات القديمة والحديثة
 * @param {string} text - النص المراد نسجه
 */function copyToClipboard(text) {
    if (!text) return;
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(() => {
            showToast("تم النسخ إلى الحافظة!");
        }).catch(err => {
            console.error("فشل النسخ:", err);
            showToast("حدث خطأ أثناء النسخ", "error");
        });
    } else {
        const textArea = document.createElement("textarea");
        textArea.value = text;
        textArea.style.position = "fixed";
        textArea.style.left = "-999999px";
        document.body.appendChild(textArea);
        textArea.select();
        try {
            document.execCommand('copy');
            showToast("تم النسخ إلى الحافظة!");
        } catch (err) {
            console.error("فشل النسخ الاحتياطي:", err);
            showToast("حدث خطأ أثناء النسخ", "error");
        }
        document.body.removeChild(textArea);
    }}/**
 * ضبط ارتفاع حقول النصوص (Textarea) تلقائياً أثناء الكتابة
 * @param {HTMLElement} element - عنصر الـ Textarea
 * @param {number} maxHeight - أقصى ارتفاع بالبكسل
 */function autoResizeTextarea(element, maxHeight = 120) {
    if (!element) return;
    element.style.height = "auto";
    element.style.height = Math.min(element.scrollHeight, maxHeight) + "px";}/**
 * فتح الصور في وضع العرض الكامل (Lightbox)
 * @param {string} src - مسار الصورة
 */function openGlobalLightbox(src) {
    let lightbox = document.getElementById("globalLightbox");
    if (!lightbox) {
        lightbox = document.createElement("div");
        lightbox.id = "globalLightbox";
        lightbox.style.cssText = `
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(0, 0, 0, 0.92);
            z-index: 99999;
            align-items: center;
            justify-content: center;
            backdrop-filter: blur(5px);
        `;
        lightbox.innerHTML = `
            <button onclick="closeGlobalLightbox()" style="position: absolute; top: 20px; right: 20px; background: rgba(255,255,255,0.15); border: none; border-radius: 50%; width: 40px; height: 40px; color: #fff; font-size: 1.2rem; cursor: pointer; display: flex; align-items: center; justify-content: center;">✕</button>
            <img id="globalLightboxImg" src="" style="max-width: 90vw; max-height: 90vh; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,0.5);" onclick="event.stopPropagation()" />
        `;
        lightbox.onclick = closeGlobalLightbox;
        document.body.appendChild(lightbox);
    }

    document.getElementById("globalLightboxImg").src = src;
    lightbox.style.display = "flex";}/**
 * إغلاق شاشة العرض الكامل
 */function closeGlobalLightbox() {
    const lightbox = document.getElementById("globalLightbox");
    if (lightbox) {
        lightbox.style.display = "none";
    }}/* ==========================================================================
   Global Compatibility Helpers (لضمان توافق جميع الصفحات والشات)
   ========================================================================== */function autoResize(element) {
    autoResizeTextarea(element);}function openLightbox(src) {
    openGlobalLightbox(src);}function closeLightbox(e) {
    if (e) e.stopPropagation();
    closeGlobalLightbox();}function toggleMobileSidebar() {
    const sidebar = document.getElementById("sidebar");
    const backdrop = document.getElementById("sidebarBackdrop");
    if (sidebar) sidebar.classList.toggle("show-mobile");
    if (backdrop) backdrop.classList.toggle("show");}/**
 * دالة مساعدة عامة لإرسال الرسائل عبر SignalR متضمنة الـ attendanceId (currentId)
 * @param {object} connection - كائن الـ SignalR Connection
 * @param {number} currentId - معرف العضو الحالي
 * @param {string} content - محتوى الرسالة
 * @param {string} mediaPath - مسار الوسائط إن وجد
 * @param {string} mediaType - نوع الوسائط إن وجد
 */async function sendChatMessage(connection, currentId, content, mediaPath = null, mediaType = null) {
    if (!connection || currentId === 0) {
        showToast("خطأ في الاتصال أو بيانات المستخدم غير صالحة", "error");
        return;
    }
    try {
        await connection.invoke("SendMessage", currentId, content, mediaPath, mediaType);
    } catch (err) {
        console.error("فشل إرسال الرسالة عبر SignalR:", err);
        showToast("فشل إرسال الرسالة", "error");
    }
}