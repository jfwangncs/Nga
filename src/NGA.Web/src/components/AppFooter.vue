<template>
  <footer class="app-footer">
    <div class="footer-content">
      <div class="footer-info">
        <p class="footer-brand">🎮 NGB Forum</p>
        <p class="copyright">© 2026 NGB Community. All rights reserved.</p>
      </div>
      <div class="footer-links">
        <a href="#" class="footer-link">关于</a>
        <span class="divider">·</span>
        <a href="#" class="footer-link">帮助</a>
        <span class="divider">·</span>
        <a href="#" class="footer-link">反馈</a>
      </div>
    </div>
  </footer>
</template>

<script setup></script>

<style scoped>
.app-footer {
  width: 100%;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  padding: 32px;
  margin-top: auto;
}

.footer-content {
  max-width: 1440px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}

.footer-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.footer-brand {
  font-size: 16px;
  font-weight: 600;
  color: #ffffff;
  margin: 0;
}

.copyright {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.7);
  margin: 0;
}

.footer-links {
  display: flex;
  align-items: center;
  gap: 12px;
}

.footer-link {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  transition: color 0.3s;
}

.footer-link:hover {
  color: #ffffff;
}

.divider {
  color: rgba(255, 255, 255, 0.4);
}

@media (max-width: 768px) {
  .app-footer {
    padding: 24px 16px;
  }

  .footer-content {
    flex-direction: column;
    text-align: center;
  }
}
</style>
