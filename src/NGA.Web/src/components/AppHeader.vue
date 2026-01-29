<template>
  <header class="app-header">
    <div class="header-content">
      <div class="header-left">
        <router-link to="/" class="logo">
          <span class="logo-icon">🎮</span>
          <span class="logo-text">NGB</span>
        </router-link>
        <nav class="nav">
          <router-link to="/" class="nav-link" active-class="active"
            >全部主题</router-link
          >
        </nav>
      </div>
      <div class="header-right">
        <UserAvatar
          avatar=""
          username="访客"
          size="small"
        />
        <span class="user-name">访客</span>
      </div>
    </div>
  </header>
</template>

<script setup>
import UserAvatar from "./UserAvatar.vue";
</script>

<style scoped>
.app-header {
  width: 100%;
  height: 64px;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-content {
  max-width: 1440px;
  height: 100%;
  margin: 0 auto;
  padding: 0 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 20px;
  font-weight: 700;
  color: #ffffff;
  text-decoration: none;
  transition: all 0.3s ease;
}

.logo:hover {
  transform: scale(1.05);
}

.logo-icon {
  font-size: 24px;
}

.logo-text {
  letter-spacing: 1px;
}

.nav {
  display: flex;
  gap: 24px;
  margin-left: 24px;
}

.nav-link {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
  transition: all 0.3s;
  padding: 6px 12px;
  border-radius: 6px;
  text-decoration: none;
}

.nav-link:hover,
.nav-link.active {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.15);
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 16px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 20px;
  backdrop-filter: blur(10px);
  transition: all 0.3s ease;
}

.header-right:hover {
  background: rgba(255, 255, 255, 0.2);
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #ffffff;
}

@media (max-width: 768px) {
  .header-content {
    padding: 0 16px;
  }

  .logo-text {
    display: none;
  }

  .nav {
    margin-left: 12px;
  }

  .user-name {
    display: none;
  }
}
</style>
