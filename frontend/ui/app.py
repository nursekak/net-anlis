import streamlit as st
import requests
import json
from datetime import datetime

# Конфигурация страницы
st.set_page_config(
    page_title="Анализатор сетевых подключений",
    page_icon="🌐",
    layout="wide"
)

# Заголовок
st.title("Анализатор сетевых подключений")

# Бэкенд URL
BACKEND_URL = "http://localhost:5000/api"

# Создаем две колонки
col1, col2 = st.columns(2)

with col1:
    st.header("Сетевые интерфейсы")
    
    # Получаем список интерфейсов
    try:
        response = requests.get(f"{BACKEND_URL}/network/interfaces")
        if response.status_code == 200:
            interfaces = response.json()
            
            # Создаем выпадающий список с интерфейсами
            interface_names = [f"{i['name']} ({i['ipAddress']})" for i in interfaces]
            selected_interface = st.selectbox(
                "Выберите сетевой интерфейс:",
                interface_names
            )
            
            # Показываем информацию о выбранном интерфейсе
            if selected_interface:
                selected_index = interface_names.index(selected_interface)
                interface = interfaces[selected_index]
                
                st.subheader("Информация об интерфейсе")
                st.write(f"**Имя:** {interface['name']}")
                st.write(f"**Описание:** {interface['description']}")
                st.write(f"**IP-адрес:** {interface['ipAddress']}")
                st.write(f"**Маска подсети:** {interface['subnetMask']}")
                st.write(f"**MAC-адрес:** {interface['macAddress']}")
                st.write(f"**Состояние:** {interface['status']}")
                
                # Конвертируем скорость из байт/с в Мбит/с
                speed_mbps = interface['speed'] * 8 / (1024 * 1024) if interface['speed'] > 0 else 0
                st.write(f"**Скорость:** {speed_mbps:.2f} Мбит/с")
                
                st.write(f"**Тип:** {interface['interfaceType']}")

                # Добавляем кнопку для тестирования скорости
                if st.button("Тестировать скорость"):
                    with st.spinner("Измерение скорости..."):
                        try:
                            # Создаем прогресс-бар
                            progress_bar = st.progress(0)
                            status_text = st.empty()
                            
                            # Тестируем скорость загрузки
                            status_text.text("Измерение скорости загрузки...")
                            response = requests.get(f"{BACKEND_URL}/network/test-speed/{interface['name']}")
                            progress_bar.progress(50)
                            
                            if response.status_code == 200:
                                speed_result = response.json()
                                progress_bar.progress(100)
                                status_text.text("Тестирование завершено!")
                                
                                st.success("Тестирование скорости завершено!")
                                st.write("**Результаты тестирования:**")
                                
                                # Отображаем результаты с иконками
                                col1, col2 = st.columns(2)
                                with col1:
                                    st.metric("Скорость загрузки", f"{speed_result['downloadSpeed']:.2f} Мбит/с", 
                                             delta=None, delta_color="normal")
                                with col2:
                                    st.metric("Скорость отдачи", f"{speed_result['uploadSpeed']:.2f} Мбит/с", 
                                             delta=None, delta_color="normal")
                                
                                st.write(f"**Время проверки:** {datetime.fromisoformat(speed_result['timestamp']).strftime('%Y-%m-%d %H:%M:%S')}")
                                
                                # Добавляем рекомендации
                                st.write("**Рекомендации:**")
                                if speed_result['downloadSpeed'] < 10:
                                    st.warning("Скорость загрузки ниже рекомендуемой (10 Мбит/с)")
                                if speed_result['uploadSpeed'] < 5:
                                    st.warning("Скорость отдачи ниже рекомендуемой (5 Мбит/с)")
                            else:
                                st.error("Ошибка при тестировании скорости")
                        except Exception as e:
                            st.error(f"Ошибка при подключении к серверу: {str(e)}")
        else:
            st.error("Ошибка при получении списка интерфейсов")
    except Exception as e:
        st.error(f"Ошибка при подключении к серверу: {str(e)}")

with col2:
    st.header("Анализ URL")
    
    # Поле для ввода URL
    url = st.text_input("Введите URL для анализа:", placeholder="https://example.com")
    
    if url:
        try:
            # Отправляем запрос на анализ URL
            response = requests.get(f"{BACKEND_URL}/network/analyze-url", params={"url": url})
            if response.status_code == 200:
                result = response.json()
                
                if not result['isValid']:
                    st.error(f"Ошибка валидации URL: {result['validationError']}")
                else:
                    st.subheader("Результаты анализа")
                    
                    # Основная информация
                    st.write("**Основная информация:**")
                    st.write(f"**Исходный URL:** {result['originalUrl']}")
                    st.write(f"**Схема (протокол):** {result['scheme']}")
                    st.write(f"**Хост:** {result['host']}")
                    st.write(f"**Порт:** {result['port']}")
                    st.write(f"**Путь:** {result['path']}")
                    
                    # Параметры запроса
                    if result['queryParameters']:
                        st.write("**Параметры запроса:**")
                        for param in result['queryParameters']:
                            st.write(f"- {param['name']}: {param['value'] if param['value'] else 'пусто'}")
                    else:
                        st.write("**Параметры запроса:** нет")
                    
                    st.write(f"**Фрагмент:** {result['fragment']}")
                    
                    # Дополнительная информация
                    st.write("**Дополнительная информация:**")
                    if result['userInfo']:
                        st.write(f"**Информация о пользователе:** {result['userInfo']}")
                    st.write(f"**Authority:** {result['authority']}")
                    st.write(f"**Absolute URI:** {result['absoluteUri']}")
                    st.write(f"**Local Path:** {result['localPath']}")
                    st.write(f"**Path and Query:** {result['pathAndQuery']}")
                    
                    # Статус и проверки
                    st.write("**Статус и проверки:**")
                    st.write(f"**Доступность:** {'✅ Доступен' if result['isAvailable'] else '❌ Недоступен'}")
                    st.write("> *Доступность означает, что сервер отвечает на ICMP-запросы (ping). Это не гарантирует, что веб-сервер работает, но показывает, что хост активен.*")
                    st.write(f"**Тип адреса:** {result['addressType']}")
                    
                    if result['dnsRecords']:
                        st.write("**DNS записи:**")
                        for record in result['dnsRecords']:
                            st.write(f"- {record}")
                    
                    # Время проверки
                    st.write(f"**Время проверки:** {datetime.fromisoformat(result['timestamp']).strftime('%Y-%m-%d %H:%M:%S')}")
            else:
                st.error("Ошибка при анализе URL")
        except Exception as e:
            st.error(f"Ошибка при подключении к серверу: {str(e)}")

# Футер
st.markdown("---")
st.markdown("Создано с использованием C# (бэкенд) и Python (фронтенд)") 